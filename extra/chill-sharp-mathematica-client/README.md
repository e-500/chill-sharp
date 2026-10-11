# ChillSharp Wolfram Language Client

A small Wolfram Language package for querying a ChillSharp API from Mathematica notebooks. It uses ChillSharp's existing HTTP/JSON API and performs filtering, ordering, and pagination on the server.

## Install or load

The directory is a Wolfram paclet. Install it from its local path, then load the context:

```wolfram
PacletInstall["path/to/chill-sharp-mathematica-client"];
Needs["ChillSharp`"]
```

For development, load the source file directly with `Get["path/to/ChillSharp.wl"]`.

## Connect and authenticate

Pass the service root URL, or a URL ending in `/api/chill`. Username/password authentication happens when the connection is created; the password is not retained in the connection state. Access-token authentication is also supported.

```wolfram
connection = ChillSharpConnect[
  "https://localhost:5001",
  "Username" -> "analyst",
  "Password" -> "your-password"
];
```

For a pre-issued token:

```wolfram
connection = ChillSharpConnect[
  "https://localhost:5001/api/chill",
  "AccessToken" -> token
];
```

`ChillSharpLogin[connection, username, password]` can authenticate or re-authenticate later. When the API returns HTTP 401 and a refresh token is available, requests refresh the token and retry once. Close the local connection when finished:

```wolfram
ChillSharpClose[connection]
```

## Query

Filters are an association from ChillSharp property names to values. Plain values use equality. Use `ChillSharpFilter[operator, value]` for another supported comparison.

```wolfram
posts = ChillSharpQuery[
  connection,
  "MyApp.Post",
  <|
    "Status" -> "Published",
    "Title" -> ChillSharpFilter["Contains", "Wolfram"],
    "CreatedAt" -> ChillSharpFilter["GreaterThanOrEqual", "2025-01-01"]
  |>,
  "Page" -> 0,
  "PageResults" -> 50,
  "SortBy" -> "CreatedAt",
  "SortDirection" -> "DESC",
  "ResultProperties" -> {"Guid", "Title", "CreatedAt"}
];
```

`Page` is zero-based. `PageResults` is the maximum number of records requested. `SortDirection` is `"ASC"` or `"DESC"`; `SortBy` defaults to `"Position"`. Set `"LogicalOperator" -> "Or"` to combine the supplied filters with OR instead of AND. The result is a `Dataset`; top-level entity fields and the entity's `Properties` are flattened into each row.

Supported filter operators are `Equal`, `NotEqual`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `Between`, `Contains`, `StartsWith`, `EndsWith`, `In`, `IsNull`, `IsNotNull`, `IsEmpty`, and `IsNotEmpty`. `Between` takes a two-item list:

```wolfram
<|"Score" -> ChillSharpFilter["Between", {10, 20}]|>
```

To get readings recorded between two dates, pass the date-time bounds as ISO 8601 strings. Replace the illustrative entity and property names with those from your API:

```wolfram
readings = ChillSharpQuery[
  connection,
  "MyApp.SensorReading",
  <|"MeasuredAt" -> ChillSharpFilter["Between", {"2025-01-01T00:00:00Z", "2025-02-01T00:00:00Z"}]|>,
  "Page" -> 0,
  "PageResults" -> 100,
  "SortBy" -> "MeasuredAt",
  "SortDirection" -> "ASC",
  "ResultProperties" -> {"MeasuredAt", "Value"}
];
```

Errors are returned as Wolfram `Failure` objects. Requests use bearer-token authentication over HTTPS when configured; use HTTPS for credentials and tokens outside local development.

## Scope

This initial client provides connection/authentication and read-only querying. It does not yet implement schema discovery, create/update/delete, attachments, or realtime subscriptions.