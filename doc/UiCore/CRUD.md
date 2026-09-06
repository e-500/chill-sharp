# CRUD Menu Configuration

Versione italiana: [Italiano](../it/UiCore/CRUD.md)

Create a menu item with `ComponentName` set to `CRUD`. Its `ComponentConfigurationJson` must be a JSON object; keys are case-insensitive. `chillType` is required. Normally it is the entity Chill type and `chillQuery` is the query Chill type that returns that entity.

When `chillQuery` is omitted or `null`, the CRUD uses automatic-query mode. The Search dialog is generated from all fields in the entity schema, every field is optional, and each populated field is sent as an `Equal` filter. Empty fields are not sent. Entity-reference selections are reduced to their GUID for equality comparison. A configured `chillQuery` continues to use its dedicated query schema and payload.

The smallest automatic-query configuration is:

```json
{
  "chillType": "Model.Post"
}
```

```json
{
  "chillType": "Model.Post",
  "chillQuery": "Query.PostQuery",
  "viewCode": "default",
  "disableAdd": false,
  "disableCreate": false,
  "disableEdit": false,
  "disableInlineEdit": false,
  "disableDelete": false,
  "defaultValues": {},
  "fixedValues": {},
  "fixedQueryValues": {},
  "defaultQueryValues": {},
  "relations": []
}
```

## Options

| Key | Type | Default | Effect |
| --- | --- | --- | --- |
| `chillType` | string | required | Entity Chill type displayed and edited by the task. |
| `chillQuery` | string or `null` | `null` | Query Chill type. When omitted, the UI generates an automatic equality-filter form from the entity schema. |
| `viewCode` | string | `default` | Schema view code used for the task. |
| `disableAdd` | boolean | `false` | Hides the Add command. |
| `disableCreate` | boolean | `false` | Prevents creating new records. |
| `disableEdit` | boolean | `false` | Prevents dialog editing. |
| `disableInlineEdit` | boolean | `false` | Prevents inline table editing. |
| `disableDelete` | boolean | `false` | Prevents deletion. |
| `defaultValues` | object | `{}` | Initial values for the create form; users may change them. |
| `fixedValues` | object | `{}` | Create values made read-only in the form and inline editor. |
| `fixedQueryValues` | object | `{}` | Mandatory query filters that the user cannot change. |
| `defaultQueryValues` | object | `{}` | Initial query values the user may change. |
| `relationLabel` | string or object | omitted | Label for this CRUD when it is opened as a relation. The object form is `{ "labelGuid", "primaryDefaultText", "secondaryDefaultText" }`. |
| `relations` | array | `[]` | Child CRUD definitions available from each row's action menu. |

Unknown JSON properties are retained by the menu editor but are not standard CRUD options.

## Values and relation placeholders

Values in the four value objects are JSON values. In a nested relation, a string `@{FieldName}` reads that property from the selected parent row, and `@{mock}` supplies a lightweight entity object for that parent row.

### Which bag is applied, and when

There are two independent flows:

| Flow | Editable starting values | Values that take precedence |
| --- | --- | --- |
| Search/query | `defaultQueryValues` | `fixedQueryValues` |
| Create entity | `defaultValues` | `fixedValues` |

UI Core merges each flow in that order. Therefore, when the same property occurs in both bags, the value in the `fixed...` bag wins. Use only the fixed bag when a value must never be changed; use only the default bag when it is merely a useful starting value. In automatic-query mode, populated query values are converted to `Equal` filters just like values entered in the generated form.

`fixedQueryValues` constrains the records returned by the child CRUD. `fixedValues` constrains the entity sent by the create flow and marks those entity properties read-only in the form and inline editor. The fixed bags are configuration, not a replacement for server-side authorization or validation: the API must still enforce tenant, ownership, and authorization rules.

### Static CLR-compatible values

The JSON literal is passed as the property value; UI Core does not evaluate or convert it before normal entity/query serialization. Use the JSON representation that the ChillSharp API expects for the CLR property.

```json
{
  "defaultQueryValues": {
    "IsPublished": true,
    "MinimumScore": 50,
    "Category": "News",
    "From": "2026-01-01T00:00:00+01:00"
  },
  "fixedValues": {
    "TenantCode": "acme",
    "Priority": 10,
    "IsInternal": false,
    "ArchivedAt": null
  }
}
```

Use JSON strings for CLR `string`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, and enum values when the server exposes those enums as strings. Use JSON numbers for numeric CLR values and JSON booleans for `bool`. Dates and GUIDs must be JSON strings, not JavaScript expressions: `"2026-01-01"` and `"8d0946dc-fc2b-4d95-b5ca-6f12d9618a5b"`, not `new Date(...)` or `Guid.NewGuid()`.

### Static entity-reference values

Yes—an entity reference can be static because a JSON object is a permitted value. Supply the same reference shape accepted by the target property, normally at least the referenced entity identifier and type:

```json
{
  "fixedQueryValues": {
    "Customer": {
      "guid": "8d0946dc-fc2b-4d95-b5ca-6f12d9618a5b",
      "chillType": "Model.Customer"
    }
  },
  "fixedValues": {
    "Status": {
      "guid": "7d8af0dd-d20d-4bb7-9a4d-f3b9d2f9c2b4",
      "chillType": "Model.OrderStatus",
      "label": "Approved"
    }
  }
}
```

`label` is optional UI metadata. Do not rely on it for identity: `guid` identifies the entity. Include `chillType` when the reference can be polymorphic or when the server/client needs explicit type information. The exact property name and object shape must match the target query or entity schema; a scalar foreign-key property instead needs the scalar GUID string, for example `"CustomerGuid": "8d0946dc-fc2b-4d95-b5ca-6f12d9618a5b"`.

### Dynamic values: `@{FieldName}` and `@{mock}`

Placeholders are resolved only while opening a relation from a selected parent row. They are not expressions and are not evaluated in a root menu CRUD, because no parent entity exists there. In that case the original placeholder string remains unchanged.

- `@{Guid}` copies the selected row's `Guid`/`guid` value.
- `@{CustomerCode}` copies a field from the selected row's `properties` object or direct object properties. A camel-case token also falls back to the Pascal-case property name.
- A missing field resolves to `null`.
- `@{mock}` creates a lightweight copy of the selected parent entity. It carries its `guid`, `chillType`, `label`, and a copy of its `properties`; it does not fetch the entity again.

Use `@{mock}` for a child reference property such as `Order`, and use `@{Guid}` for a scalar foreign key such as `OrderGuid`:

```json
{
  "relations": [
    {
      "chillType": "Model.OrderRow",
      "chillQuery": "Query.OrderRowQuery",
      "fixedQueryValues": { "Order": "@{mock}" },
      "defaultQueryValues": { "Order": "@{mock}" },
      "fixedValues": { "Order": "@{mock}" },
      "defaultValues": { "Order": "@{mock}" }
    },
    {
      "chillType": "Model.OrderRow",
      "chillQuery": "Query.OrderRowQuery",
      "fixedQueryValues": { "OrderGuid": "@{Guid}" },
      "fixedValues": { "OrderGuid": "@{Guid}" }
    }
  ]
}
```

Use the first form only when the query/entity schema exposes an `Order` entity-reference property. Use the second form only when it exposes an `OrderGuid` scalar property. Do not send `@{mock}` to a scalar GUID property.

```json
{
  "chillType": "Model.Order",
  "chillQuery": "Query.OrderQuery",
  "relations": [
    {
      "chillType": "Model.OrderRow",
      "chillQuery": "Query.OrderRowQuery",
      "relationLabel": {
        "labelGuid": "ORDER-ROWS-LABEL",
        "primaryDefaultText": "Rows",
        "secondaryDefaultText": "Righe"
      },
      "fixedQueryValues": { "Order": "@{mock}" },
      "defaultQueryValues": { "Order": "@{mock}" },
      "defaultValues": { "Order": "@{mock}" },
      "fixedValues": { "Order": "@{mock}" }
    }
  ]
}
```

Use `fixedQueryValues` for an unchangeable parent filter and `defaultQueryValues` for an initial editable filter. Use `fixedValues` for a read-only create value and `defaultValues` for an editable create default. Relations can contain further `relations` arrays.

## Persisting the menu entry

The menu API stores the JSON as a string. For example, use `ComponentName: "CRUD"` and serialize the object into `ComponentConfigurationJson` when calling `set-menu`. Invalid JSON, or a JSON array instead of an object, cannot configure the task.
