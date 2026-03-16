from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any
from urllib.parse import quote

import requests

from .exceptions import ChillSharpClientError


JsonDict = dict[str, Any]


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
        timeout: float = 30.0,
        session: requests.Session | None = None,
    ) -> None:
        self._base_url = self._normalize_required_value(base_url, "base_url").rstrip("/")
        self._username = username.strip() if username else None
        self._password = password.strip() if password else None
        self._culture_name = culture_name.strip() if culture_name else None
        self._timeout = timeout
        self.session = session or requests.Session()
        self._token_state = _TokenState(access_token=access_token.strip() if access_token else None)

    def query(self, dto_query: JsonDict) -> JsonDict:
        return self._send_json("POST", self._build_chill_url("query"), dto_query)

    def find(self, dto_entity: JsonDict) -> JsonDict | None:
        return self._send_json("POST", self._build_chill_url("find"), dto_entity)

    def create(self, dto_entity: JsonDict) -> JsonDict:
        return self._send_json("POST", self._build_chill_url("create"), dto_entity)

    def update(self, dto_entity: JsonDict) -> JsonDict:
        return self._send_json("POST", self._build_chill_url("update"), dto_entity)

    def delete(self, dto_entity: JsonDict) -> None:
        self._send_json("POST", self._build_chill_url("delete"), dto_entity, expect_response_body=False)

    def chunk(self, operations: list[JsonDict]) -> list[JsonDict]:
        return self._send_json("POST", self._build_chill_url("chunk"), operations)

    def get_schema(self, chill_type: str, chill_view_code: str, culture_name: str | None = None) -> JsonDict | None:
        encoded_type = quote(self._normalize_required_value(chill_type, "chill_type"))
        encoded_view = quote(self._normalize_required_value(chill_view_code, "chill_view_code"))
        effective_culture_name = culture_name.strip() if culture_name else self._culture_name
        url = self._build_chill_url(f"get-schema?chillType={encoded_type}&chillViewCode={encoded_view}")
        if effective_culture_name:
            url += f"&cultureName={quote(effective_culture_name)}"
        return self._send_json("GET", url)

    def set_schema(self, schema: JsonDict) -> JsonDict:
        return self._send_json("POST", self._build_chill_url("set-schema"), schema)

    def get_text(self, label_guid: str, culture_name: str) -> JsonDict | None:
        encoded_guid = quote(self._normalize_required_value(label_guid, "label_guid"))
        encoded_culture = quote(self._normalize_required_value(culture_name, "culture_name"))
        return self._send_json("GET", self._build_i18n_url(f"text/{encoded_guid}/{encoded_culture}"))

    def set_text(self, payload: JsonDict) -> JsonDict:
        return self._send_json("PUT", self._build_i18n_url("text"), payload)

    def register_auth_account(self, payload: JsonDict) -> JsonDict:
        response = self._send_auth_json("POST", "account/register", payload, allow_anonymous=True)
        self._apply_auth_token(response, forget_password=True)
        return response

    def login_auth_account(self, payload: JsonDict) -> JsonDict:
        response = self._send_auth_json("POST", "account/login", payload, allow_anonymous=True)
        self._apply_auth_token(response, forget_password=True)
        return response

    def refresh_auth_account(self) -> JsonDict:
        return self._get_auth_token_if_necessary(force_refresh=True)

    def change_auth_password(self, payload: JsonDict) -> JsonDict:
        return self._send_auth_json("POST", "account/change-password", payload)

    def request_auth_password_reset(self, payload: JsonDict) -> JsonDict:
        return self._send_auth_json("POST", "account/request-password-reset", payload, allow_anonymous=True)

    def reset_auth_password(self, payload: JsonDict) -> JsonDict:
        return self._send_auth_json("POST", "account/reset-password", payload, allow_anonymous=True)

    def _send_auth_json(
        self,
        method: str,
        relative_url: str,
        payload: Any = None,
        expect_response_body: bool = True,
        allow_anonymous: bool = False,
    ) -> Any:
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
            return self._send_json(
                method,
                url,
                payload,
                expect_response_body=expect_response_body,
                allow_anonymous=allow_anonymous,
                allow_retry=False,
            )

        if not response.ok:
            raise ChillSharpClientError(
                f"HTTP {response.status_code} calling {method} {url}",
                status_code=response.status_code,
                response_text=response.text,
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

    def _get_auth_token_if_necessary(self, force_refresh: bool = False) -> JsonDict:
        if not force_refresh and self._has_usable_access_token() and not self._should_refresh_access_token():
            return self._create_current_token_response()

        if self._token_state.refresh_token and (not force_refresh or not self._password):
            try:
                refreshed = self._send_auth_json(
                    "POST",
                    "account/refresh",
                    {"RefreshToken": self._token_state.refresh_token},
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
                "account/login",
                {
                    "UserNameOrEmail": self._username,
                    "Password": self._password,
                },
                allow_anonymous=True,
            )
            self._apply_auth_token(token, forget_password=True)
            return token

        if self._has_usable_access_token():
            return self._create_current_token_response()

        raise ChillSharpClientError("No auth token is available and the client cannot obtain a new one.")

    def _apply_auth_token(self, payload: JsonDict, forget_password: bool) -> None:
        self._token_state.access_token = self._get_payload_value(payload, "AccessToken")
        self._token_state.access_token_issued_utc = self._parse_datetime(
            self._get_payload_value(payload, "AccessTokenIssuedUtc")
        )
        self._token_state.access_token_expires_utc = self._parse_datetime(
            self._get_payload_value(payload, "AccessTokenExpiresUtc")
        )
        self._token_state.refresh_token = self._get_payload_value(payload, "RefreshToken")
        self._token_state.refresh_token_expires_utc = self._parse_datetime(
            self._get_payload_value(payload, "RefreshTokenExpiresUtc")
        )

        user_name = self._get_payload_value(payload, "UserName")
        if user_name:
            self._username = str(user_name).strip()

        if forget_password:
            self._password = None

    def _can_use_authentication(self) -> bool:
        return bool(
            self._token_state.access_token
            or self._token_state.refresh_token
            or (self._username and self._password)
        )

    def _has_usable_access_token(self) -> bool:
        if not self._token_state.access_token:
            return False

        if not self._token_state.access_token_expires_utc:
            return True

        return datetime.now(timezone.utc) < self._token_state.access_token_expires_utc

    def _should_refresh_access_token(self) -> bool:
        issued = self._token_state.access_token_issued_utc
        expires = self._token_state.access_token_expires_utc
        if not issued or not expires:
            return False

        if expires <= issued:
            return True

        refresh_threshold = issued + (expires - issued) * 0.75
        return datetime.now(timezone.utc) >= refresh_threshold

    def _try_refresh_authentication(self) -> bool:
        if not self._token_state.refresh_token and not self._password:
            return False

        try:
            self._get_auth_token_if_necessary(force_refresh=True)
            return True
        except ChillSharpClientError:
            return False

    def _create_current_token_response(self) -> JsonDict:
        return {
            "AccessToken": self._token_state.access_token or "",
            "AccessTokenIssuedUtc": self._format_datetime(self._token_state.access_token_issued_utc),
            "AccessTokenExpiresUtc": self._format_datetime(self._token_state.access_token_expires_utc),
            "RefreshToken": self._token_state.refresh_token or "",
            "RefreshTokenExpiresUtc": self._format_datetime(self._token_state.refresh_token_expires_utc),
            "UserName": self._username or "",
        }

    def _build_chill_url(self, relative_url: str) -> str:
        return f"{self._base_url}/{relative_url.lstrip('/')}"

    def _build_auth_url(self, relative_url: str) -> str:
        return f"{self._get_auth_base_url().rstrip('/')}/{relative_url.lstrip('/')}"

    def _build_i18n_url(self, relative_url: str) -> str:
        return f"{self._get_i18n_base_url().rstrip('/')}/{relative_url.lstrip('/')}"

    def _get_auth_base_url(self) -> str:
        suffix = "/chill"
        if self._base_url.lower().endswith(suffix):
            return self._base_url[: -len(suffix)] + "/chill-auth"
        return self._base_url.rstrip("/") + "-auth"

    def _get_i18n_base_url(self) -> str:
        suffix = "/chill"
        if self._base_url.lower().endswith(suffix):
            return self._base_url[: -len(suffix)] + "/chill-i18n"
        return self._base_url.rstrip("/") + "-i18n"

    @staticmethod
    def _normalize_required_value(value: str, argument_name: str) -> str:
        normalized = value.strip() if value else ""
        if not normalized:
            raise ValueError(f"{argument_name} is required.")
        return normalized

    @staticmethod
    def _parse_datetime(value: Any) -> datetime | None:
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
        if value is None:
            return ""
        return value.astimezone(timezone.utc).isoformat().replace("+00:00", "Z")

    @staticmethod
    def _get_payload_value(payload: JsonDict, key: str) -> Any:
        if key in payload:
            return payload[key]

        camel_key = key[0].lower() + key[1:]
        if camel_key in payload:
            return payload[camel_key]

        return None
