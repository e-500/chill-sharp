# Generating Client Libraries

Versione italiana: [Italiano](../it/ClientGeneration/README.md)

This section explains how to generate non-.NET client libraries for a ChillSharp host.

Targets covered here:

- TypeScript
- Python

For ready-to-use generic clients already included in this repository, see:

- [../../ext/chill-sharp-ts-client/README.md](../../ext/chill-sharp-ts-client/README.md)
- [../../ext/chill-sharp-react-client/README.md](../../ext/chill-sharp-react-client/README.md)
- [../../ext/chill-sharp-vue-client/README.md](../../ext/chill-sharp-vue-client/README.md)
- [../../ext/chill-sharp-ng-client/README.md](../../ext/chill-sharp-ng-client/README.md)
- [../../ext/chill-sharp-py-client/README.md](../../ext/chill-sharp-py-client/README.md)

Those packages are generic wrappers around the standard ChillSharp HTTP API. The rest of this document covers host-specific client generation from OpenAPI.

## Important Constraint

ChillSharp does not automatically publish an OpenAPI document by itself.

Client generation therefore depends on the host application exposing one through normal ASP.NET Core Swagger/OpenAPI tooling.

## 1. Expose OpenAPI In The Host

Add Swagger generation to the host application:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapChillApi();
```

With that in place, a standard Swagger JSON document is typically available at:

```text
/swagger/v1/swagger.json
```

Example:

```text
http://localhost:5000/swagger/v1/swagger.json
```

## 2. Decide What The Generated Client Should Cover

A ChillSharp host may expose several surfaces:

- core Chill API
- auth/account endpoints
- auth-management endpoints
- i18n endpoints

If all modules are registered in one host and Swagger is enabled globally, the generated OpenAPI document can include all of them.

## 3. Generate A TypeScript Client

One practical option is `openapi-generator-cli`.

Install or use it through your preferred package manager, then run:

```bash
openapi-generator-cli generate \
  -i http://localhost:5000/swagger/v1/swagger.json \
  -g typescript-fetch \
  -o generated/ts-client
```

Other useful TypeScript generators include:

- `typescript-axios`
- `typescript-angular`

Example:

```bash
openapi-generator-cli generate \
  -i http://localhost:5000/swagger/v1/swagger.json \
  -g typescript-axios \
  -o generated/ts-client
```

## 4. Generate A Python Client

Using the same OpenAPI document:

```bash
openapi-generator-cli generate \
  -i http://localhost:5000/swagger/v1/swagger.json \
  -g python \
  -o generated/python-client
```

This produces a Python package with request models and API wrappers based on the published OpenAPI description.

## 5. Host-Specific Notes

Generated clients are only as accurate as the host’s OpenAPI document.

That means:

- if the host does not expose Swagger, there is nothing to generate from
- if the host excludes some controllers, those endpoints will not appear in the generated client
- if auth is enabled, the generated client still needs bearer-token handling configured by the consuming app

## 6. Recommended Workflow

For TypeScript and Python, the recommended workflow is:

1. build your ChillSharp host
2. add Swagger/OpenAPI to the host
3. run the host locally or in CI
4. export `/swagger/v1/swagger.json`
5. generate the client library
6. publish or commit the generated client as appropriate for your project

## 7. When To Prefer `ChillSharp.Client`

If the consumer is .NET, prefer `ChillSharp.Client`.

Use generated TypeScript or Python clients when:

- the frontend is browser-based and not .NET
- you need Python-based automation or integration
- you want strongly typed clients for non-.NET environments

If you do not need generated, host-specific types, you can also use the generic clients shipped in `ext/`:

- `ext/chill-sharp-ts-client`
- `ext/chill-sharp-react-client`
- `ext/chill-sharp-vue-client`
- `ext/chill-sharp-ng-client`
- `ext/chill-sharp-py-client`

## 8. Stability Guidance

If you plan to generate clients regularly:

- keep your host’s public routes stable
- version the API
- regenerate clients as part of release workflow
- treat OpenAPI shape changes as public-contract changes


