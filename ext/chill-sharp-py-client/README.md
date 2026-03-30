# chill-sharp-py-client

Python client for a generic ChillSharp service.

This package targets the standard ChillSharp HTTP surface:

- core Chill API at `/api/chill`
- schema API at `/api/chill-schema`
- auth API at `/api/chill-auth`
- i18n API at `/api/chill-i18n`

It is intentionally lightweight. Payloads are plain Python dictionaries so the client can work against arbitrary ChillSharp models without code generation.

## Install

From the repository root:

```bash
pip install -e ext/chill-sharp-py-client
```

Or inside the package folder:

```bash
cd ext/chill-sharp-py-client
pip install .
```

## Quick Start

```python
from chillsharp_py_client import ChillSharpClient

client = ChillSharpClient("http://localhost:5000/api/chill", culture_name="it-IT")

created = client.create({
    "ChillType": "Model.Post",
    "Guid": "00000000-0000-0000-0000-000000000001",
    "Properties": {
        "Title": "Hello",
        "Author": "Ada Lovelace",
    },
})

found = client.find({
    "ChillType": "Model.Post",
    "Guid": created["Guid"],
})
```

## Construction Modes

### Anonymous or externally authenticated

```python
client = ChillSharpClient("http://localhost:5000/api/chill", culture_name="it-IT")
```

### With an existing access token

```python
client = ChillSharpClient(
    "http://localhost:5000/api/chill",
    access_token="your-jwt-token",
    culture_name="it-IT",
)
```

### With username and password

```python
client = ChillSharpClient(
    "http://localhost:5000/api/chill",
    username="root",
    password="Pass123$",
    culture_name="it-IT",
)
```

If the service supports ChillSharp auth endpoints, the client can log in and refresh tokens automatically.

## Core ChillSharp Operations

### Query

```python
result = client.query({
    "ChillType": "Query.PostQuery",
    "Properties": {
        "Title": "Hello"
    },
    "ResultProperties": [
        {"Name": "Guid"},
        {"Name": "Title"},
        {"Name": "Author"},
    ],
})
```

### Find

```python
entity = client.find({
    "ChillType": "Model.Post",
    "Guid": "f2d5d5e3-0a1f-4d15-9396-2ab5f6c4ff11",
})
```

### Create

```python
entity = client.create({
    "ChillType": "Model.Post",
    "Guid": "f2d5d5e3-0a1f-4d15-9396-2ab5f6c4ff11",
    "Properties": {
        "Title": "New title",
        "Author": "Grace Hopper",
    },
})
```

### Update

```python
updated = client.update({
    "ChillType": "Model.Post",
    "Guid": "f2d5d5e3-0a1f-4d15-9396-2ab5f6c4ff11",
    "Properties": {
        "Title": "Updated title",
    },
})
```

### Delete

```python
client.delete({
    "ChillType": "Model.Post",
    "Guid": "f2d5d5e3-0a1f-4d15-9396-2ab5f6c4ff11",
})
```

### Chunk

Use `chunk()` when several operations should be sent in one HTTP request.
The operations are executed in `Index` order when you provide it. For write-heavy batches, set `Index` explicitly.

```python
operations = client.chunk([
    {
        "Index": 0,
        "Verb": "create",
        "Entity": {
            "ChillType": "Model.Post",
            "Guid": "11111111-1111-1111-1111-111111111111",
            "Properties": {"Title": "First", "Author": "A"},
        },
    },
    {
        "Index": 1,
        "Verb": "create",
        "Entity": {
            "ChillType": "Model.Post",
            "Guid": "22222222-2222-2222-2222-222222222222",
            "Properties": {"Title": "Second", "Author": "B"},
        },
    },
    {
        "Index": 2,
        "Verb": "update",
        "Entity": {
            "ChillType": "Model.Post",
            "Guid": "11111111-1111-1111-1111-111111111111",
            "Properties": {"Title": "First updated"},
        },
    },
])
```

### Chunk inside one transaction

Wrap the batch with `transaction` and `commit` when all write operations must succeed or fail together.

```python
operations = client.chunk([
    {
        "Index": 0,
        "Verb": "transaction",
    },
    {
        "Index": 1,
        "Verb": "create",
        "Entity": {
            "ChillType": "Model.Blog",
            "Guid": "33333333-3333-3333-3333-333333333333",
            "Properties": {
                "Name": "Batch blog",
                "Url": "https://example.local/batch-blog",
            },
        },
    },
    {
        "Index": 2,
        "Verb": "create",
        "Entity": {
            "ChillType": "Model.Post",
            "Guid": "44444444-4444-4444-4444-444444444444",
            "Properties": {
                "Title": "Batch post",
                "Author": "Grace Hopper",
            },
        },
    },
    {
        "Index": 3,
        "Verb": "commit",
    },
])
```

Use this pattern only for the operations that must share the same database transaction. If one write fails before `commit`, the transaction is not committed.

## Schema Operations

### Get schema

```python
schema = client.get_schema("Model.Post", "default")

# Override the constructor default for one call
english_schema = client.get_schema("Model.Post", "default", culture_name="en-GB")
```

### Get schema list

```python
schema_list = client.get_schema_list()
english_schema_list = client.get_schema_list(culture_name="en-GB")
```

### Set schema

```python
client.set_schema({
    "ChillType": "Model.Post",
    "ChillViewCode": "default",
    "DisplayName": "Post",
    "Properties": [
        {
            "Name": "Title",
            "DisplayName": "Post title",
        }
    ],
})
```

### Get entity options

```python
options = client.get_entity_options("Model.Post")
```

### Set entity options

```python
options = client.set_entity_options({
    "ChillType": "Model.Post",
    "ChecksumEnabled": True,
    "LabelFormatString": "{Title}",
    "ShortLabelFormatString": "{Title}",
    "FullTextContentFormatString": "{Title} {Author}",
    "ChangeLogEnabled": True,
})
```

## I18n Operations

### Get text

```python
text = client.get_text({
    "LabelGuid": "4e16f6c0-6b95-4d67-98bc-9f4d0d63eaf1",
    "CultureName": "it-IT",
    "PrimaryCultureName": "en-GB",
    "PrimaryDefaultText": "Blog title",
    "SecondaryCultureName": "it-IT",
    "SecondaryDefaultText": "Titolo del blog",
})
```

### Set text

```python
saved = client.set_text({
    "LabelGuid": "4e16f6c0-6b95-4d67-98bc-9f4d0d63eaf1",
    "CultureName": "it-IT",
    "Value": "Titolo del blog",
})
```

## Auth Operations

The client assumes the auth base path is derived from `/api/chill` to `/api/chill-auth`, matching the .NET client.

### Register account

```python
token = client.register_auth_account({
    "UserName": "root",
    "Email": "root@example.com",
    "Password": "Pass123$",
    "DisplayName": "Root",
    "DisplayCultureName": "it-IT",
    "CreateChillAuthUser": True,
})
```

If `DisplayCultureName` is provided and `CreateChillAuthUser` is `True`, the server presets the linked `AuthUser` with culture-based defaults for `DisplayTimeZone`, `DisplayDateFormat`, and `DisplayNumberFormat`.

### Login

```python
token = client.login_auth_account({
    "UserNameOrEmail": "root",
    "Password": "Pass123$",
})
```

### Refresh current token

```python
token = client.refresh_auth_account()
```

### Change password

```python
result = client.change_auth_password({
    "CurrentPassword": "Pass123$",
    "NewPassword": "Pass456$",
})
```

### Request password reset

```python
reset_token = client.request_auth_password_reset({
    "UserNameOrEmail": "root",
})
```

### Reset password

```python
result = client.reset_auth_password({
    "UserId": reset_token["UserId"],
    "ResetToken": reset_token["ResetToken"],
    "NewPassword": "Pass789$",
})
```

## Auth Management Operations

Use these endpoints when the host exposes ChillSharp auth management APIs.

### Get current permissions

```python
permissions = client.get_auth_permissions()
```

### Get user list

```python
users = client.get_auth_user_list()
```

Auth user list/detail payloads include `DisplayCultureName`, `DisplayTimeZone`, `DisplayDateFormat`, and `DisplayNumberFormat`.

### Get managed user

```python
user = client.get_auth_user("f2d5d5e3-0a1f-4d15-9396-2ab5f6c4ff11")
```

### Set managed user

```python
user = client.set_auth_user({
    "Guid": None,
    "ExternalId": "identity-user-001",
    "UserName": "identity.user",
    "DisplayName": "Identity User",
    "DisplayCultureName": "it-IT",
    "DisplayTimeZone": "W. Europe Standard Time",
    "DisplayDateFormat": "DD/MM/YYYY",
    "DisplayNumberFormat": "1.000,00",
    "IsActive": True,
    "CanManagePermissions": False,
    "CanManageSchema": True,
    "RoleGuids": [],
    "Permissions": [],
})
```

### Get role list

```python
roles = client.get_auth_role_list()
```

### Get managed role

```python
role = client.get_auth_role("e2f0d8d5-0a1f-4d15-9396-2ab5f6c4ff22")
```

### Set managed role

```python
role = client.set_auth_role({
    "Guid": None,
    "Name": "Editors",
    "Description": "Can edit posts",
    "IsActive": True,
    "UserGuids": [],
    "Permissions": [],
})
```

## Accessing The Underlying Session

If you need custom headers, proxies, or retries, use the exposed `session`:

```python
client.session.headers["X-Correlation-Id"] = "demo-123"
```

## Error Handling

All request failures raise `ChillSharpClientError`.

```python
from chillsharp_py_client import ChillSharpClient, ChillSharpClientError

client = ChillSharpClient("http://localhost:5000/api/chill", culture_name="it-IT")

try:
    client.get_schema("Model.Post", "default")
except ChillSharpClientError as exc:
    print(exc.status_code)
    print(exc.response_text)
```

## Generic Payload Strategy

This package does not generate Python model classes for your Chill entities.

That is intentional:

- ChillSharp models are application-specific
- the standard Chill API already works well with generic dictionaries
- a generic client is easier to reuse across many different ChillSharp services

If you need strongly typed Python clients, generate them from your host OpenAPI document as described in [doc/ClientGeneration/README.md](../../doc/ClientGeneration/README.md).






