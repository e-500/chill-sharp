"""
ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core
and turns an existing data model into a fully working REST API with almost no setup.
Copyright (C) 2025 Andrea Piovesan

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
"""

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime, timezone
from enum import IntEnum
from typing import Any, TypedDict
from urllib.parse import quote

import requests

from .exceptions import ChillSharpClientError


CHILL_SHARP_PY_CLIENT_VERSION = "0.1.0"
API_BASE_PATH = "api/"
JsonDict = dict[str, Any]
ATTACHMENT_ENTITY_CHILL_TYPE = "ChillSharp.Attachment.Model.Attachment"
ATTACHMENT_QUERY_CHILL_TYPE = "ChillSharp.Attachment.Query.AttachmentQuery"


class ChillUserPreferences(TypedDict):
    """Display preferences resolved for the current authenticated user."""

    displayCultureName: str
    displayTimeZone: str
    displayDateFormat: str
    displayNumberFormat: str


class PermissionEffect(IntEnum):
    """Effect applied by a permission rule."""

    ALLOW = 1
    DENY = 2


class PermissionAction(IntEnum):
    """Action controlled by a permission rule."""

    FULL_CONTROL = 0
    QUERY = 1
    CREATE = 2
    UPDATE = 3
    DELETE = 4
    SEE = 5
    MODIFY = 6


class PermissionScope(IntEnum):
    """Hierarchy level targeted by a permission rule."""

    MODULE = 1
    ENTITY = 2
    PROPERTY = 3


@dataclass
class _TokenState:
    access_token: str | None = None
    access_token_issued_utc: datetime | None = None
    access_token_expires_utc: datetime | None = None
    refresh_token: str | None = None
    refresh_token_expires_utc: datetime | None = None


class ChillSharpClient:
    """Session-based Python client for a generic ChillSharp service."""

    def __init__(
        self,
        base_url: str,
        access_token: str | None = None,
        username: str | None = None,
        password: str | None = None,
        culture_name: str | None = None,
        api_base_path: str = API_BASE_PATH,
        timeout: float = 30.0,
        session: requests.Session | None = None,
    ) -> None:
        """Initialize the client with endpoint, auth, and session settings."""
        self._base_url = self._normalize_base_url(base_url, api_base_path)
        self._username = username.strip() if username else None
        self._password = password.strip() if password else None
        self._culture_name = culture_name.strip() if culture_name else None
        self._timeout = timeout
        self.session = session or requests.Session()
        self._token_state = _TokenState(access_token=access_token.strip() if access_token else None)

    def query(self, dto_query: JsonDict) -> JsonDict:
        """Send a query request to the Chill endpoint."""
        return self._send_json("POST", self._build_chill_url("query"), dto_query)

    def lookup(self, dto_query: JsonDict) -> JsonDict:
        """Send a generic full-text lookup request to the Chill endpoint."""
        return self._send_json("POST", self._build_chill_url("lookup"), dto_query)

    def find(self, dto_entity: JsonDict) -> JsonDict | None:
        """Fetch a single entity matching the supplied DTO."""
        return self._send_json("POST", self._build_chill_url("find"), dto_entity)

    def create(self, dto_entity: JsonDict) -> JsonDict:
        """Create a new entity through the Chill endpoint."""
        return self._send_json("POST", self._build_chill_url("create"), dto_entity)

    def update(self, dto_entity: JsonDict) -> JsonDict:
        """Update an existing entity through the Chill endpoint."""
        return self._send_json("POST", self._build_chill_url("update"), dto_entity)

    def delete(self, dto_entity: JsonDict) -> None:
        """Delete an entity through the Chill endpoint."""
        self._send_json("POST", self._build_chill_url("delete"), dto_entity, expect_response_body=False)

    def autocomplete(self, dto: JsonDict) -> JsonDict:
        """Apply autocomplete logic to an entity or query DTO."""
        return self._send_json("POST", self._build_chill_url("autocomplete"), dto)

    def validate(self, dto: JsonDict) -> list[JsonDict]:
        """Validate an entity or query DTO and return the validation errors."""
        response = self._send_json("POST", self._build_chill_url("validate"), dto)
        return response if isinstance(response, list) else []

    def chunk(self, operations: list[JsonDict]) -> list[JsonDict]:
        """Submit a batch of operations in one request."""
        return self._send_json("POST", self._build_chill_url("chunk"), operations)

    def upload_attachment(
        self,
        target_entity: JsonDict,
        file: JsonDict,
        title: str | None = None,
        description: str | None = None,
        is_public: bool = False,
    ) -> list[JsonDict]:
        """Upload one attachment payload for a Chill entity."""
        return self.upload_attachments(target_entity, [file], title=title, description=description, is_public=is_public)

    def upload_attachments(
        self,
        target_entity: JsonDict,
        files: list[JsonDict],
        title: str | None = None,
        description: str | None = None,
        is_public: bool = False,
    ) -> list[JsonDict]:
        """Upload one or more attachment payloads for a Chill entity."""
        target = self._get_attachment_target_info(target_entity)
        if not isinstance(files, list) or len(files) == 0:
            raise ValueError("files is required.")

        data = {
            "attachToChillType": target["chillType"],
            "attachToGuid": target["guid"],
            "public": "true" if is_public else "false",
        }
        if title and title.strip():
            data["title"] = title.strip()
        if description and description.strip():
            data["description"] = description.strip()

        multipart_files: list[tuple[str, tuple[str, bytes, str]]] = []
        for file in files:
            file_name = self._normalize_required_value(
                self._coerce_string(self._get_payload_value(file, "fileName")),
                "file.fileName",
            )
            content = self._coerce_attachment_content(self._get_payload_value(file, "content"))
            content_type = self._normalize_optional_value(self._coerce_string(self._get_payload_value(file, "contentType"))) or "application/octet-stream"
            multipart_files.append(("file", (file_name, content, content_type)))

        response = self._send_multipart_request(
            "POST",
            self._build_attachment_url("attachment/upload"),
            data=data,
            files=multipart_files,
            allow_retry=False,
        )
        try:
            parsed = response.json()
        except ValueError as exc:
            raise ChillSharpClientError(
                f"Invalid JSON response from POST {self._build_attachment_url('attachment/upload')}",
                status_code=response.status_code,
                response_text=response.text,
            ) from exc

        return parsed if isinstance(parsed, list) else []

    def get_attachments(self, target_entity: JsonDict) -> list[JsonDict]:
        """Load attachment entities linked to one Chill entity."""
        target = self._get_attachment_target_info(target_entity)
        response = self.query(
            {
                "chillType": ATTACHMENT_QUERY_CHILL_TYPE,
                "properties": {
                    "attachToChillType": target["chillType"],
                    "attachToGuid": target["guid"],
                },
            }
        )
        results = self._get_payload_value(response, "results")
        return results if isinstance(results, list) else []

    def download_attachment(self, attachment_or_guid: JsonDict | str) -> bytes:
        """Download attachment content by guid or attachment entity."""
        attachment_guid = (
            self._normalize_required_value(attachment_or_guid, "attachment_guid")
            if isinstance(attachment_or_guid, str)
            else self._get_attachment_guid(attachment_or_guid)
        )

        response = self._send_request(
            "GET",
            self._build_attachment_url(f"attachment/download?guid={quote(attachment_guid)}"),
            None,
            allow_anonymous=not self._can_use_authentication(),
        )
        return response.content

    def version(self) -> str:
        """Return the Python client library version."""
        return CHILL_SHARP_PY_CLIENT_VERSION

    def test(self) -> str:
        """Call the Chill test endpoint and return the raw response text."""
        return self._send_text("GET", self._build_api_url("test"), allow_anonymous=True)

    def get_schema(self, chill_type: str, chill_view_code: str, culture_name: str | None = None, update: bool = False) -> JsonDict | None:
        """Retrieve schema metadata for a type and view."""
        encoded_type = quote(self._normalize_required_value(chill_type, "chill_type"))
        encoded_view = quote(self._normalize_required_value(chill_view_code, "chill_view_code"))
        effective_culture_name = culture_name.strip() if culture_name else self._culture_name
        url = self._build_schema_url(f"get-schema?chillType={encoded_type}&chillViewCode={encoded_view}")
        if effective_culture_name:
            url += f"&cultureName={quote(effective_culture_name)}"
        if update:
            url += "&update=true"
        return self._send_json("GET", url)

    def set_schema(self, schema: JsonDict) -> JsonDict:
        """Persist schema metadata through the Chill endpoint."""
        return self._send_json("POST", self._build_schema_url("set-schema"), schema)

    def get_schema_list(self, culture_name: str | None = None) -> list[JsonDict]:
        """Retrieve the list of registered entities and queries."""
        effective_culture_name = culture_name.strip() if culture_name else self._culture_name
        url = self._build_schema_url("get-schema-list")
        if effective_culture_name:
            url += f"?cultureName={quote(effective_culture_name)}"
        response = self._send_json("GET", url)
        return response if isinstance(response, list) else []

    def get_entity_options(self, chill_type: str) -> JsonDict:
        """Retrieve runtime entity options for a Chill type."""
        encoded_type = quote(self._normalize_required_value(chill_type, "chill_type"))
        return self._send_json("GET", self._build_schema_url(f"get-entity-options?chillType={encoded_type}"))

    def set_entity_options(self, entity_options: JsonDict) -> JsonDict:
        """Persist runtime entity options for a Chill type."""
        return self._send_json("POST", self._build_schema_url("set-entity-options"), entity_options)

    def get_text(self, request: JsonDict) -> JsonDict | None:
        """Retrieve an i18n text entry using a request payload."""
        return self._send_json(
            "POST",
            self._build_i18n_url("get-text"),
            self._prepare_get_text_request(request),
            allow_anonymous=True,
        )

    def get_texts(self, requests: list[JsonDict]) -> list[JsonDict | None]:
        """Retrieve multiple i18n text entries in a single request."""
        if not isinstance(requests, list):
            raise ValueError("requests is required.")

        response = self._send_json(
            "POST",
            self._build_i18n_url("get-multiple-text"),
            [self._prepare_get_text_request(request) for request in requests],
            allow_anonymous=True,
        )
        return response if isinstance(response, list) else []

    def set_text(self, payload: JsonDict) -> JsonDict:
        """Persist an i18n text payload."""
        return self._send_json("PUT", self._build_i18n_url("set-text"), payload)

    def register_auth_account(self, payload: JsonDict) -> JsonDict:
        """Register an auth account and store returned tokens."""
        response = self._send_auth_json("POST", "register", payload, allow_anonymous=True)
        self._apply_auth_token(response, forget_password=True)
        return response

    def login_auth_account(self, payload: JsonDict) -> JsonDict:
        """Log in an auth account and store returned tokens."""
        response = self._send_auth_json("POST", "login", payload, allow_anonymous=True)
        self._apply_auth_token(response, forget_password=True)
        return response

    def refresh_auth_account(self) -> JsonDict:
        """Force a refresh of the current auth token state."""
        return self._get_auth_token_if_necessary(force_refresh=True)

    def logout_auth_account(self) -> None:
        """Log out the current auth account and clear local token state."""
        self._send_auth_json("POST", "logout", expect_response_body=False)
        self._clear_auth_token()

    def change_auth_password(self, payload: JsonDict) -> JsonDict:
        """Change the password for the current auth account."""
        return self._send_auth_json("POST", "change-password", payload)

    def request_auth_password_reset(self, payload: JsonDict) -> JsonDict:
        """Start the password reset flow for an auth account."""
        return self._send_auth_json("POST", "request-password-reset", payload, allow_anonymous=True)

    def reset_auth_password(self, payload: JsonDict) -> JsonDict:
        """Complete the password reset flow with a reset payload."""
        return self._send_auth_json("POST", "reset-password", payload, allow_anonymous=True)

    def get_auth_permissions(self) -> JsonDict:
        """Return the current user permissions together with role permissions."""
        return self._send_auth_json("GET", "get-permissions")

    def get_current_user_preferences(self) -> ChillUserPreferences:
        """Return the culture, time-zone, date-format, and number-format of the authenticated user."""
        response = self._send_auth_json("GET", "current-user-preferences")
        if not isinstance(response, dict):
            raise ChillSharpClientError("Unexpected current user preferences response.")
        return response  # type: ignore[return-value]

    def get_auth_user_list(self) -> list[JsonDict]:
        """Return the full auth user list."""
        response = self._send_auth_json("GET", "get-user-list")
        return response if isinstance(response, list) else []

    def get_auth_user(self, user_guid: str) -> JsonDict:
        """Return one auth user with assigned roles and direct permissions."""
        normalized_user_guid = self._normalize_required_value(user_guid, "user_guid")
        user = self._send_auth_json("GET", f"users/{quote(normalized_user_guid)}")
        roles = self.get_auth_user_roles(normalized_user_guid)
        permissions = self.get_auth_permission_rules(user_guid=normalized_user_guid)
        return {
            **user,
            "roles": roles,
            "permissions": permissions,
        }

    def set_auth_user(self, payload: JsonDict) -> JsonDict:
        """Create or update an auth user with the full role and permission list."""
        user_guid = self._normalize_optional_value(self._coerce_string(self._get_payload_value(payload, "guid"))) or None
        base_payload = {
            "externalId": self._coerce_string(self._get_payload_value(payload, "externalId")),
            "userName": self._coerce_string(self._get_payload_value(payload, "userName")),
            "displayName": self._coerce_string(self._get_payload_value(payload, "displayName")),
            "displayCultureName": self._coerce_string(self._get_payload_value(payload, "displayCultureName")),
            "displayTimeZone": self._coerce_string(self._get_payload_value(payload, "displayTimeZone")),
            "displayDateFormat": self._coerce_string(self._get_payload_value(payload, "displayDateFormat")),
            "displayNumberFormat": self._coerce_string(self._get_payload_value(payload, "displayNumberFormat")),
            "isActive": bool(self._get_payload_value(payload, "isActive")),
            "canManagePermissions": bool(self._get_payload_value(payload, "canManagePermissions")),
            "canManageSchema": bool(self._get_payload_value(payload, "canManageSchema")),
            "menuHierarchy": self._coerce_string(self._get_payload_value(payload, "menuHierarchy")),
        }

        if user_guid:
            user = self.update_auth_user(user_guid, base_payload)
        else:
            user = self.create_auth_user(
                {
                    **base_payload,
                    "email": self._coerce_string(self._get_payload_value(payload, "email")),
                }
            )

        if not user:
            raise ChillSharpClientError("Auth user was not found after set_auth_user execution.")

        role_guids = self._coerce_guid_list(self._get_payload_value(payload, "roleGuids"))
        permissions = self._coerce_permission_items(self._get_payload_value(payload, "permissions"))
        self._sync_user_roles(user["guid"], role_guids)
        self._sync_user_permissions(user["guid"], permissions)
        return self.get_auth_user(user["guid"])

    def get_auth_role_list(self) -> list[JsonDict]:
        """Return the full auth role list."""
        response = self._send_auth_json("GET", "get-role-list")
        return response if isinstance(response, list) else []

    def get_auth_module_list(self) -> list[str]:
        """Return the distinct auth module list."""
        response = self._send_auth_json("GET", "get-module-list")
        return response if isinstance(response, list) else []

    def get_auth_entity_list(self, module: str | None = None) -> list[str]:
        """Return the distinct entity list for an optional auth module."""
        normalized_module = self._normalize_optional_value(module)
        suffix = f"?module={quote(normalized_module)}" if normalized_module is not None else ""
        response = self._send_auth_json("GET", f"get-entity-list{suffix}")
        return response if isinstance(response, list) else []

    def get_auth_query_list(self, module: str | None = None) -> list[str]:
        """Return the distinct query list for an optional auth module."""
        normalized_module = self._normalize_optional_value(module)
        suffix = f"?module={quote(normalized_module)}" if normalized_module is not None else ""
        response = self._send_auth_json("GET", f"get-query-list{suffix}")
        return response if isinstance(response, list) else []

    def get_auth_module_entity_list(self, module: str | None = None) -> list[str]:
        """Compatibility alias for get_auth_entity_list."""
        return self.get_auth_entity_list(module)

    def get_auth_property_list(self, chill_type: str) -> list[str]:
        """Return the distinct property list for a Chill type."""
        normalized_chill_type = self._normalize_required_value(chill_type, "chill_type")
        response = self._send_auth_json("GET", f"get-property-list?chillType={quote(normalized_chill_type)}")
        return response if isinstance(response, list) else []

    def get_auth_role(self, role_guid: str) -> JsonDict:
        """Return one auth role with assigned users and direct permissions."""
        normalized_role_guid = self._normalize_required_value(role_guid, "role_guid")
        role = self._send_auth_json("GET", f"roles/{quote(normalized_role_guid)}")
        permissions = self.get_auth_permission_rules(role_guid=normalized_role_guid)
        users = self._get_users_assigned_to_role(normalized_role_guid)
        return {
            **role,
            "users": users,
            "permissions": permissions,
        }

    def set_auth_role(self, payload: JsonDict) -> JsonDict:
        """Create or update an auth role with the full user and permission list."""
        role_guid = self._normalize_optional_value(self._coerce_string(self._get_payload_value(payload, "guid"))) or None
        base_payload = {
            "name": self._coerce_string(self._get_payload_value(payload, "name")),
            "description": self._coerce_string(self._get_payload_value(payload, "description")),
            "isActive": bool(self._get_payload_value(payload, "isActive")),
            "menuHierarchy": self._coerce_string(self._get_payload_value(payload, "menuHierarchy")),
        }

        role = self.update_auth_role(role_guid, base_payload) if role_guid else self.create_auth_role(base_payload)
        if not role:
            raise ChillSharpClientError("Auth role was not found after set_auth_role execution.")

        user_guids = self._coerce_guid_list(self._get_payload_value(payload, "userGuids"))
        permissions = self._coerce_permission_items(self._get_payload_value(payload, "permissions"))
        self._sync_role_users(role["guid"], user_guids)
        self._sync_role_permissions(role["guid"], permissions)
        return self.get_auth_role(role["guid"])

    def get_auth_users(self) -> list[JsonDict]:
        """Return all auth users."""
        response = self._send_auth_json("GET", "users")
        return response if isinstance(response, list) else []

    def create_auth_user(self, payload: JsonDict) -> JsonDict:
        """Create a new auth user."""
        return self._send_auth_json("POST", "users", payload)

    def update_auth_user(self, user_guid: str, payload: JsonDict) -> JsonDict | None:
        """Update an existing auth user."""
        normalized_user_guid = self._normalize_required_value(user_guid, "user_guid")
        return self._send_auth_json("PUT", f"users/{quote(normalized_user_guid)}", payload)

    def delete_auth_user(self, user_guid: str) -> None:
        """Delete an auth user."""
        normalized_user_guid = self._normalize_required_value(user_guid, "user_guid")
        self._send_auth_json("DELETE", f"users/{quote(normalized_user_guid)}", expect_response_body=False)

    def get_auth_user_roles(self, user_guid: str) -> list[JsonDict]:
        """Return the roles assigned to a user."""
        normalized_user_guid = self._normalize_required_value(user_guid, "user_guid")
        response = self._send_auth_json("GET", f"users/{quote(normalized_user_guid)}/roles")
        return response if isinstance(response, list) else []

    def assign_auth_role(self, user_guid: str, role_guid: str) -> None:
        """Assign a role to a user."""
        normalized_user_guid = self._normalize_required_value(user_guid, "user_guid")
        normalized_role_guid = self._normalize_required_value(role_guid, "role_guid")
        self._send_auth_json("PUT", f"users/{quote(normalized_user_guid)}/roles/{quote(normalized_role_guid)}", expect_response_body=False)

    def remove_auth_role(self, user_guid: str, role_guid: str) -> None:
        """Remove a role from a user."""
        normalized_user_guid = self._normalize_required_value(user_guid, "user_guid")
        normalized_role_guid = self._normalize_required_value(role_guid, "role_guid")
        self._send_auth_json("DELETE", f"users/{quote(normalized_user_guid)}/roles/{quote(normalized_role_guid)}", expect_response_body=False)

    def get_auth_roles(self) -> list[JsonDict]:
        """Return all auth roles."""
        response = self._send_auth_json("GET", "roles")
        return response if isinstance(response, list) else []

    def create_auth_role(self, payload: JsonDict) -> JsonDict:
        """Create a new auth role."""
        return self._send_auth_json("POST", "roles", payload)

    def update_auth_role(self, role_guid: str, payload: JsonDict) -> JsonDict | None:
        """Update an existing auth role."""
        normalized_role_guid = self._normalize_required_value(role_guid, "role_guid")
        return self._send_auth_json("PUT", f"roles/{quote(normalized_role_guid)}", payload)

    def delete_auth_role(self, role_guid: str) -> None:
        """Delete an auth role."""
        normalized_role_guid = self._normalize_required_value(role_guid, "role_guid")
        self._send_auth_json("DELETE", f"roles/{quote(normalized_role_guid)}", expect_response_body=False)

    def get_auth_permission_rules(
        self,
        user_guid: str | None = None,
        role_guid: str | None = None,
    ) -> list[JsonDict]:
        """Return permission rules filtered by optional user or role."""
        query_parts: list[str] = []
        normalized_user_guid = self._normalize_optional_value(user_guid)
        normalized_role_guid = self._normalize_optional_value(role_guid)
        if normalized_user_guid:
            query_parts.append(f"userGuid={quote(normalized_user_guid)}")
        if normalized_role_guid:
            query_parts.append(f"roleGuid={quote(normalized_role_guid)}")
        suffix = f"?{'&'.join(query_parts)}" if query_parts else ""
        response = self._send_auth_json("GET", f"permissions{suffix}")
        return response if isinstance(response, list) else []

    def get_auth_permission_rule(self, rule_guid: str) -> JsonDict | None:
        """Return one permission rule by identifier."""
        normalized_rule_guid = self._normalize_required_value(rule_guid, "rule_guid")
        return self._send_auth_json("GET", f"permissions/{quote(normalized_rule_guid)}")

    def create_auth_permission_rule(self, payload: JsonDict) -> JsonDict:
        """Create a permission rule."""
        return self._send_auth_json("POST", "permissions", payload)

    def update_auth_permission_rule(self, rule_guid: str, payload: JsonDict) -> JsonDict | None:
        """Update a permission rule."""
        normalized_rule_guid = self._normalize_required_value(rule_guid, "rule_guid")
        return self._send_auth_json("PUT", f"permissions/{quote(normalized_rule_guid)}", payload)

    def delete_auth_permission_rule(self, rule_guid: str) -> None:
        """Delete a permission rule."""
        normalized_rule_guid = self._normalize_required_value(rule_guid, "rule_guid")
        self._send_auth_json("DELETE", f"permissions/{quote(normalized_rule_guid)}", expect_response_body=False)

    def _prepare_get_text_request(self, request: JsonDict) -> JsonDict:
        """Normalize a get-text request and apply the client default culture when needed."""
        if request is None:
            raise ValueError("request is required.")

        label_guid = self._normalize_required_value(
            self._coerce_string(self._get_payload_value(request, "labelGuid")),
            "labelGuid",
        )
        culture_name = self._coerce_string(self._get_payload_value(request, "cultureName")) or self._culture_name or ""
        normalized_culture_name = self._normalize_required_value(culture_name, "cultureName")

        return {
            "labelGuid": label_guid,
            "cultureName": normalized_culture_name,
            "primaryCultureName": self._coerce_string(self._get_payload_value(request, "primaryCultureName")),
            "primaryDefaultText": self._coerce_string(self._get_payload_value(request, "primaryDefaultText")),
            "secondaryCultureName": self._coerce_string(self._get_payload_value(request, "secondaryCultureName")),
            "secondaryDefaultText": self._coerce_string(self._get_payload_value(request, "secondaryDefaultText")),
        }

    def _send_auth_json(
        self,
        method: str,
        relative_url: str,
        payload: Any = None,
        expect_response_body: bool = True,
        allow_anonymous: bool = False,
    ) -> Any:
        """Send a JSON request to the auth service."""
        return self._send_json(
            method,
            self._build_auth_url(relative_url),
            payload,
            expect_response_body=expect_response_body,
            allow_anonymous=allow_anonymous,
        )

    def _send_json(
        self,
        method: str,
        url: str,
        payload: Any = None,
        expect_response_body: bool = True,
        allow_anonymous: bool = False,
        allow_retry: bool = True,
    ) -> Any:
        """Send a JSON request and normalize the HTTP response handling."""
        response = self._send_request(
            method,
            url,
            payload,
            allow_anonymous=allow_anonymous,
            allow_retry=allow_retry,
        )

        if not expect_response_body or not response.text.strip():
            return None

        try:
            return response.json()
        except ValueError as exc:
            raise ChillSharpClientError(
                f"Invalid JSON response from {method} {url}",
                status_code=response.status_code,
                response_text=response.text,
            ) from exc

    def _send_text(
        self,
        method: str,
        url: str,
        allow_anonymous: bool = False,
        allow_retry: bool = True,
    ) -> str:
        """Send a request and return the raw response text."""
        response = self._send_request(
            method,
            url,
            None,
            allow_anonymous=allow_anonymous,
            allow_retry=allow_retry,
        )
        return response.text

    def _send_multipart_request(
        self,
        method: str,
        url: str,
        data: dict[str, str],
        files: list[tuple[str, tuple[str, bytes, str]]],
        allow_anonymous: bool = False,
        allow_retry: bool = True,
    ) -> requests.Response:
        """Send a multipart/form-data request and normalize auth and retry behavior."""
        if not allow_anonymous and self._can_use_authentication():
            self._get_auth_token_if_necessary()

        headers: JsonDict = {}
        if not allow_anonymous and self._token_state.access_token:
            headers["Authorization"] = f"Bearer {self._token_state.access_token}"

        response = self.session.request(
            method=method,
            url=url,
            data=data,
            files=files,
            headers=headers,
            timeout=self._timeout,
        )

        if response.status_code in (401, 403) and not allow_anonymous and allow_retry and self._try_refresh_authentication():
            return self._send_multipart_request(
                method,
                url,
                data,
                files,
                allow_anonymous=allow_anonymous,
                allow_retry=False,
            )

        if not response.ok:
            raise ChillSharpClientError(
                f"HTTP {response.status_code} calling {method} {url}",
                status_code=response.status_code,
                response_text=response.text,
            )

        return response

    def _send_request(
        self,
        method: str,
        url: str,
        payload: Any = None,
        allow_anonymous: bool = False,
        allow_retry: bool = True,
    ) -> requests.Response:
        """Send an HTTP request and normalize auth and retry behavior."""
        if not allow_anonymous and self._can_use_authentication():
            self._get_auth_token_if_necessary()

        headers: JsonDict = {}
        if not allow_anonymous and self._token_state.access_token:
            headers["Authorization"] = f"Bearer {self._token_state.access_token}"

        response = self.session.request(
            method=method,
            url=url,
            json=payload,
            headers=headers,
            timeout=self._timeout,
        )

        if response.status_code in (401, 403) and not allow_anonymous and allow_retry and self._try_refresh_authentication():
            return self._send_request(
                method,
                url,
                payload,
                allow_anonymous=allow_anonymous,
                allow_retry=False,
            )

        if not response.ok:
            raise ChillSharpClientError(
                f"HTTP {response.status_code} calling {method} {url}",
                status_code=response.status_code,
                response_text=response.text,
            )

        return response

    def _get_auth_token_if_necessary(self, force_refresh: bool = False) -> JsonDict:
        """Return a valid auth token, refreshing or logging in when needed."""
        if not force_refresh and self._has_usable_access_token() and not self._should_refresh_access_token():
            return self._create_current_token_response()

        if self._token_state.refresh_token and (not force_refresh or not self._password):
            try:
                refreshed = self._send_auth_json(
                    "POST",
                    "refresh",
                    {"refreshToken": self._token_state.refresh_token},
                    allow_anonymous=True,
                )
                self._apply_auth_token(refreshed, forget_password=True)
                return refreshed
            except ChillSharpClientError:
                self._token_state.refresh_token = None
                self._token_state.refresh_token_expires_utc = None

        if self._username and self._password:
            token = self._send_auth_json(
                "POST",
                "login",
                {
                    "userNameOrEmail": self._username,
                    "password": self._password,
                },
                allow_anonymous=True,
            )
            self._apply_auth_token(token, forget_password=True)
            return token

        if self._has_usable_access_token():
            return self._create_current_token_response()

        raise ChillSharpClientError("No auth token is available and the client cannot obtain a new one.")

    def _apply_auth_token(self, payload: JsonDict, forget_password: bool) -> None:
        """Copy auth token fields from a response payload into local state."""
        self._token_state.access_token = self._coerce_string(self._get_payload_value(payload, "accessToken")) or None
        self._token_state.access_token_issued_utc = self._parse_datetime(
            self._get_payload_value(payload, "accessTokenIssuedUtc")
        )
        self._token_state.access_token_expires_utc = self._parse_datetime(
            self._get_payload_value(payload, "accessTokenExpiresUtc")
        )
        self._token_state.refresh_token = self._coerce_string(self._get_payload_value(payload, "refreshToken")) or None
        self._token_state.refresh_token_expires_utc = self._parse_datetime(
            self._get_payload_value(payload, "refreshTokenExpiresUtc")
        )

        user_name = self._coerce_string(self._get_payload_value(payload, "userName"))
        if user_name:
            self._username = user_name

        if forget_password:
            self._password = None

    def _clear_auth_token(self) -> None:
        """Clear local auth token state."""
        self._token_state = _TokenState()

    def _can_use_authentication(self) -> bool:
        """Check whether the client has enough information to authenticate."""
        return bool(
            self._token_state.access_token
            or self._token_state.refresh_token
            or (self._username and self._password)
        )

    def _has_usable_access_token(self) -> bool:
        """Determine whether the current access token is still usable."""
        if not self._token_state.access_token:
            return False

        if not self._token_state.access_token_expires_utc:
            return True

        return datetime.now(timezone.utc) < self._token_state.access_token_expires_utc

    def _should_refresh_access_token(self) -> bool:
        """Determine whether the access token is old enough to refresh."""
        issued = self._token_state.access_token_issued_utc
        expires = self._token_state.access_token_expires_utc
        if not issued or not expires:
            return False

        if expires <= issued:
            return True

        refresh_threshold = issued + (expires - issued) * 0.75
        return datetime.now(timezone.utc) >= refresh_threshold

    def _try_refresh_authentication(self) -> bool:
        """Try to refresh authentication without surfacing errors."""
        if not self._token_state.refresh_token and not self._password:
            return False

        try:
            self._get_auth_token_if_necessary(force_refresh=True)
            return True
        except ChillSharpClientError:
            return False

    def _create_current_token_response(self) -> JsonDict:
        """Build a token payload from the current local auth state."""
        return {
            "accessToken": self._token_state.access_token or "",
            "accessTokenIssuedUtc": self._format_datetime(self._token_state.access_token_issued_utc),
            "accessTokenExpiresUtc": self._format_datetime(self._token_state.access_token_expires_utc),
            "refreshToken": self._token_state.refresh_token or "",
            "refreshTokenExpiresUtc": self._format_datetime(self._token_state.refresh_token_expires_utc),
            "userId": "",
            "userName": self._username or "",
        }

    def _get_users_assigned_to_role(self, role_guid: str) -> list[JsonDict]:
        """Resolve the users currently assigned to a role."""
        matches: list[JsonDict] = []
        for user in self.get_auth_users():
            roles = self.get_auth_user_roles(self._coerce_string(self._get_payload_value(user, "guid")))
            if any(self._coerce_string(self._get_payload_value(role, "guid")) == role_guid for role in roles):
                matches.append(user)
        return matches

    def _sync_user_roles(self, user_guid: str, role_guids: list[str]) -> None:
        """Synchronize a user's assigned roles with the provided target list."""
        desired = set(role_guids)
        current_roles = self.get_auth_user_roles(user_guid)
        current = {self._coerce_string(self._get_payload_value(role, "guid")) for role in current_roles}

        for role_guid in desired - current:
            self.assign_auth_role(user_guid, role_guid)
        for role_guid in current - desired:
            self.remove_auth_role(user_guid, role_guid)

    def _sync_user_permissions(self, user_guid: str, permissions: list[JsonDict]) -> None:
        """Synchronize a user's direct permissions."""
        current = self.get_auth_permission_rules(user_guid=user_guid)
        self._sync_permission_rules(
            current,
            permissions,
            lambda item: {
                "userGuid": user_guid,
                "roleGuid": None,
                "effect": self._get_payload_value(item, "effect"),
                "action": self._get_payload_value(item, "action"),
                "scope": self._get_payload_value(item, "scope"),
                "module": self._coerce_string(self._get_payload_value(item, "module")),
                "entityName": self._normalize_nullable_payload_string(item, "entityName"),
                "propertyName": self._normalize_nullable_payload_string(item, "propertyName"),
                "appliesToAllProperties": bool(self._get_payload_value(item, "appliesToAllProperties")),
                "description": self._coerce_string(self._get_payload_value(item, "description")),
            },
        )

    def _sync_role_users(self, role_guid: str, user_guids: list[str]) -> None:
        """Synchronize the users assigned to a role."""
        desired = set(user_guids)
        current_users = self._get_users_assigned_to_role(role_guid)
        current = {self._coerce_string(self._get_payload_value(user, "guid")) for user in current_users}

        for user_guid in desired - current:
            self.assign_auth_role(user_guid, role_guid)
        for user_guid in current - desired:
            self.remove_auth_role(user_guid, role_guid)

    def _sync_role_permissions(self, role_guid: str, permissions: list[JsonDict]) -> None:
        """Synchronize a role's direct permissions."""
        current = self.get_auth_permission_rules(role_guid=role_guid)
        self._sync_permission_rules(
            current,
            permissions,
            lambda item: {
                "userGuid": None,
                "roleGuid": role_guid,
                "effect": self._get_payload_value(item, "effect"),
                "action": self._get_payload_value(item, "action"),
                "scope": self._get_payload_value(item, "scope"),
                "module": self._coerce_string(self._get_payload_value(item, "module")),
                "entityName": self._normalize_nullable_payload_string(item, "entityName"),
                "propertyName": self._normalize_nullable_payload_string(item, "propertyName"),
                "appliesToAllProperties": bool(self._get_payload_value(item, "appliesToAllProperties")),
                "description": self._coerce_string(self._get_payload_value(item, "description")),
            },
        )

    def _sync_permission_rules(
        self,
        current_rules: list[JsonDict],
        desired_rules: list[JsonDict],
        build_payload: Any,
    ) -> None:
        """Synchronize permission rules using create, update, and delete operations."""
        desired_by_guid: dict[str, JsonDict] = {}
        new_rules: list[JsonDict] = []

        for rule in desired_rules:
            guid = self._normalize_optional_value(self._coerce_string(self._get_payload_value(rule, "guid")))
            if guid:
                desired_by_guid[guid] = rule
            else:
                new_rules.append(rule)

        for current_rule in current_rules:
            current_guid = self._coerce_string(self._get_payload_value(current_rule, "guid"))
            desired_rule = desired_by_guid.pop(current_guid, None)
            if desired_rule is None:
                self.delete_auth_permission_rule(current_guid)
                continue
            self.update_auth_permission_rule(current_guid, build_payload(desired_rule))

        for desired_rule in desired_by_guid.values():
            self.create_auth_permission_rule(build_payload(desired_rule))
        for desired_rule in new_rules:
            self.create_auth_permission_rule(build_payload(desired_rule))

    def _build_chill_url(self, relative_url: str) -> str:
        """Build an absolute URL for the core Chill service."""
        return f"{self._base_url}/{relative_url.lstrip('/')}"

    def _build_api_url(self, relative_url: str) -> str:
        """Build an absolute URL for the configured ChillSharp API base path."""
        return f"{self._get_api_base_url().rstrip('/')}/{relative_url.lstrip('/')}"

    def _build_auth_url(self, relative_url: str) -> str:
        """Build an absolute URL for the auth service."""
        return f"{self._get_auth_base_url().rstrip('/')}/{relative_url.lstrip('/')}"

    def _build_schema_url(self, relative_url: str) -> str:
        """Build an absolute URL for the schema service."""
        return f"{self._get_schema_base_url().rstrip('/')}/{relative_url.lstrip('/')}"

    def _build_i18n_url(self, relative_url: str) -> str:
        """Build an absolute URL for the i18n service."""
        return f"{self._get_i18n_base_url().rstrip('/')}/{relative_url.lstrip('/')}"

    def _build_attachment_url(self, relative_url: str) -> str:
        """Build an absolute URL for the attachment service."""
        return f"{self._get_attachment_base_url().rstrip('/')}/{relative_url.lstrip('/')}"

    def _get_auth_base_url(self) -> str:
        """Resolve the auth service base URL from the Chill base URL."""
        suffix = "/chill"
        if self._base_url.lower().endswith(suffix):
            return self._base_url[: -len(suffix)] + "/chill-auth"
        return self._base_url.rstrip("/") + "-auth"

    def _get_schema_base_url(self) -> str:
        """Resolve the schema service base URL from the Chill base URL."""
        suffix = "/chill"
        if self._base_url.lower().endswith(suffix):
            return self._base_url[: -len(suffix)] + "/chill-schema"
        return self._base_url.rstrip("/") + "-schema"

    def _get_i18n_base_url(self) -> str:
        """Resolve the i18n service base URL from the Chill base URL."""
        suffix = "/chill"
        if self._base_url.lower().endswith(suffix):
            return self._base_url[: -len(suffix)] + "/chill-i18n"
        return self._base_url.rstrip("/") + "-i18n"

    def _get_attachment_base_url(self) -> str:
        """Resolve the attachment service base URL from the Chill base URL."""
        suffix = "/chill"
        if self._base_url.lower().endswith(suffix):
            return self._base_url[: -len(suffix)] + "/chill-attachment"
        return self._base_url.rstrip("/") + "-attachment"

    def _get_api_base_url(self) -> str:
        """Resolve the configured API base URL from the Chill base URL."""
        suffix = "/chill"
        if self._base_url.lower().endswith(suffix):
            return self._base_url[: -len(suffix)]
        return self._base_url.rstrip("/")

    @classmethod
    def _normalize_base_url(cls, base_url: str, api_base_path: str | None) -> str:
        """Normalize a server root, API base URL, or specific ChillSharp endpoint URL."""
        normalized = cls._normalize_required_value(base_url, "base_url").rstrip("/")
        if cls._is_known_chillsharp_endpoint_base(normalized):
            return normalized

        normalized_api_base_path = cls._normalize_api_base_path(api_base_path)
        if not normalized_api_base_path:
            return f"{normalized}/chill"

        if cls._ends_with_path_segment(normalized, normalized_api_base_path):
            return f"{normalized}/chill"

        return f"{normalized}/{normalized_api_base_path}/chill"

    @staticmethod
    def _normalize_api_base_path(api_base_path: str | None) -> str:
        normalized = api_base_path.strip() if api_base_path else API_BASE_PATH
        return normalized.strip("/")

    @staticmethod
    def _is_known_chillsharp_endpoint_base(base_url: str) -> bool:
        lower_base_url = base_url.lower()
        return (
            lower_base_url.endswith("/chill")
            or lower_base_url.endswith("/chill-auth")
            or lower_base_url.endswith("/chill-schema")
            or lower_base_url.endswith("/chill-i18n")
            or lower_base_url.endswith("/chill-attachment")
        )

    @staticmethod
    def _ends_with_path_segment(value: str, segment: str) -> bool:
        return value.lower().endswith(f"/{segment.lower()}")

    @staticmethod
    def _normalize_required_value(value: str, argument_name: str) -> str:
        """Validate that a required string argument is present."""
        normalized = value.strip() if value else ""
        if not normalized:
            raise ValueError(f"{argument_name} is required.")
        return normalized

    @staticmethod
    def _normalize_optional_value(value: str | None) -> str | None:
        """Normalize an optional string argument while preserving empty strings."""
        if value is None:
            return None
        return value.strip()

    @staticmethod
    def _normalize_nullable_payload_string(payload: JsonDict, key: str) -> str | None:
        """Read an optional payload string and normalize blank values to None."""
        value = ChillSharpClient._coerce_string(ChillSharpClient._get_payload_value(payload, key))
        return value or None

    @staticmethod
    def _coerce_string(value: Any) -> str:
        """Normalize arbitrary values into trimmed strings for endpoint payloads."""
        if value is None:
            return ""
        return str(value).strip()

    @staticmethod
    def _coerce_guid_list(value: Any) -> list[str]:
        """Normalize a JSON list into a list of non-empty guid strings."""
        if not isinstance(value, list):
            return []
        result: list[str] = []
        for item in value:
            normalized = ChillSharpClient._coerce_string(item)
            if normalized:
                result.append(normalized)
        return result

    @staticmethod
    def _coerce_permission_items(value: Any) -> list[JsonDict]:
        """Normalize a JSON list into a list of permission-rule payload dictionaries."""
        if not isinstance(value, list):
            return []
        return [item for item in value if isinstance(item, dict)]

    def _get_attachment_target_info(self, target_entity: JsonDict) -> JsonDict:
        """Extract the guid and ChillType required by attachment helpers."""
        guid = self._normalize_required_value(
            self._coerce_string(self._get_payload_value(target_entity, "guid")),
            "target_entity.guid",
        )
        chill_type = self._normalize_required_value(
            self._coerce_string(self._get_payload_value(target_entity, "chillType")),
            "target_entity.chillType",
        )
        return {
            "guid": guid,
            "chillType": chill_type,
        }

    def _get_attachment_guid(self, attachment_entity: JsonDict) -> str:
        """Extract and validate the guid of an attachment DTO."""
        guid = self._normalize_required_value(
            self._coerce_string(self._get_payload_value(attachment_entity, "guid")),
            "attachment_entity.guid",
        )
        chill_type = self._normalize_optional_value(self._coerce_string(self._get_payload_value(attachment_entity, "chillType")))
        if chill_type:
            normalized_chill_type = chill_type.split(".")[-1]
            normalized_attachment_type = ATTACHMENT_ENTITY_CHILL_TYPE.split(".")[-1]
            if normalized_chill_type != normalized_attachment_type:
                raise ValueError("attachment_entity must point to an attachment.")
        return guid

    @staticmethod
    def _coerce_attachment_content(value: Any) -> bytes:
        """Normalize attachment content into bytes for multipart uploads."""
        if isinstance(value, bytes):
            return value
        if isinstance(value, bytearray):
            return bytes(value)
        if isinstance(value, str):
            return value.encode("utf-8")
        if hasattr(value, "read"):
            content = value.read()
            if isinstance(content, bytes):
                return content
            if isinstance(content, str):
                return content.encode("utf-8")
        raise ValueError("file.content must be bytes, bytearray, str, or a readable stream.")

    @staticmethod
    def _parse_datetime(value: Any) -> datetime | None:
        """Parse an ISO datetime value and normalize it to UTC."""
        if not value:
            return None
        text = str(value).strip()
        if not text:
            return None
        if text.endswith("Z"):
            text = text[:-1] + "+00:00"
        parsed = datetime.fromisoformat(text)
        if parsed.tzinfo is None:
            parsed = parsed.replace(tzinfo=timezone.utc)
        return parsed.astimezone(timezone.utc)

    @staticmethod
    def _format_datetime(value: datetime | None) -> str:
        """Format a UTC datetime value for API responses."""
        if value is None:
            return ""
        return value.astimezone(timezone.utc).isoformat().replace("+00:00", "Z")

    @staticmethod
    def _get_payload_value(payload: JsonDict, key: str) -> Any:
        """Read a payload field using camelCase or PascalCase keys."""
        if key in payload:
            return payload[key]

        pascal_key = key[0].upper() + key[1:]
        if pascal_key in payload:
            return payload[pascal_key]

        for candidate_key, value in payload.items():
            if candidate_key.lower() == key.lower():
                return value

        return None






