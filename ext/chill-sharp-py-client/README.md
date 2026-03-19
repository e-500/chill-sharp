# chill-sharp-py-client

Python client for a generic ChillSharp service.

This package targets the standard ChillSharp HTTP surface:

- core Chill API at `/api/chill`
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

```python
operations = client.chunk([
    {
        "Verb": "CREATE",
        "Entity": {
            "ChillType": "Model.Post",
            "Guid": "11111111-1111-1111-1111-111111111111",
            "Properties": {"Title": "First", "Author": "A"},
        },
    },
    {
        "Verb": "CREATE",
        "Entity": {
            "ChillType": "Model.Post",
            "Guid": "22222222-2222-2222-2222-222222222222",
            "Properties": {"Title": "Second", "Author": "B"},
        },
    },
])
```

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
    "CreateChillAuthUser": True,
})
```

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

