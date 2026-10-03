# Automatic Query System

Versione italiana: [Italiano](../it/AutomaticQuery/README.md)

The automatic query system builds strongly typed LINQ expression trees from structured filter definitions. It is an opt-in alternative for cases where creating a custom `ChillQuery.OnQuery(...)` implementation for every combination of filters would be repetitive.

The existing query API remains unchanged. `ChillEngine.Query(...)` continues to accept `IChillQuery<IChillEntity>`, and existing `ChillQuery` subclasses continue to work as before.

Automatic query definitions are available through `ChillDtoQuery` and the existing `POST /api/chill/query` endpoint. Schema discovery does not yet advertise operator compatibility for each property, so clients must currently build the definition from their knowledge of the entity model.

## Main Types

The prototype provides two related entry points:

- `AutomaticQuery` contains a structured filter definition and can apply it to any compatible `IQueryable<T>`.
- `AutomaticQuery<TEntity>` derives from `ChillQuery` and lets the definition run through the standard `ChillEngine.Query(...)` pipeline.

Supporting types are:

- `AutomaticQueryGroup`, which combines filters and nested groups with `And` or `Or`.
- `AutomaticQueryFilter`, which identifies a property path, operator, and value.
- `AutomaticQueryOperator`, which defines the available comparisons.

All types are in the `ChillSharp.EF` namespace.

## Use The Shared Query Endpoint

Normal and automatic queries share `POST /api/chill/query`. The presence of `AutomaticQuery` selects the execution mode:

- Without `AutomaticQuery`, `ChillType` identifies a registered query type such as `Query.PostQuery`.
- With `AutomaticQuery`, `ChillType` identifies the target entity type such as `Model.Post`.

```json
{
  "chillType": "Model.Post",
  "automaticQuery": {
    "filter": {
      "logicalOperator": "And",
      "filters": [
        {
          "propertyName": "Title",
          "operator": "Contains",
          "value": "release",
          "ignoreCase": true
        }
      ],
      "groups": []
    }
  },
  "properties": {
    "FullTextSearch": ""
  },
  "ordering": {
    "propertyName": "Position",
    "direction": "ASC"
  },
  "pagination": {
    "page": 1,
    "pageResults": 25
  },
  "resultProperties": [
    { "propertyName": "Guid" },
    { "propertyName": "Title" }
  ]
}
```

Operator and logical-operator names are serialized as strings. Existing numeric enum values remain readable by the default converter.

The endpoint authorizes an automatic query against the entity named by `ChillType`. Registered queries continue to be resolved to their related entity before the same query permission is checked. Chunk operations use the same distinction.

The response uses the normal `ChillDtoQuery` shape, preserves the `AutomaticQuery` definition, and populates `Results` normally. Existing clients that omit `AutomaticQuery` require no payload changes.

The bundled .NET and TypeScript client contracts expose the same optional field and filter types. The MCP query contract also preserves the definition and checks automatic queries against the MCP visibility of the target entity.

### Client availability

| Client | Automatic-query surface |
| --- | --- |
| C# | `ChillSharp.Client.Dto.AutomaticQuery`, group, filter, and enum DTOs; `ChillDtoQuery.AutomaticQuery` |
| TypeScript | Native exported `AutomaticQuery` types and a typed `ChillSharpClient.query(...)` overload |
| Angular | Re-exports all automatic-query types and provides a typed `Observable<ChillDtoQuery>` overload |
| React | Re-exports all automatic-query types and types `useQueryMutation()` with `ChillDtoQuery` input/output |
| Vue | Re-exports all automatic-query types and types `useQueryMutation()` with `ChillDtoQuery` input/output |
| Python | Exported `AutomaticQuery`, group, filter, operator, and `ChillDtoQuery` `TypedDict` definitions |

The React, Vue, and Angular packages delegate execution to the TypeScript client, so they use the same JSON contract and endpoint behavior.

## Execute Through `ChillEngine.Query`

Use `AutomaticQuery<TEntity>` when the target is a `ChillEntity`:

```csharp
using ChillSharp.EF;

var query = new AutomaticQuery<Post>
{
    Definition = new AutomaticQuery
    {
        Filter = new AutomaticQueryGroup
        {
            Filters =
            {
                new AutomaticQueryFilter
                {
                    PropertyName = nameof(Post.Title),
                    Operator = AutomaticQueryOperator.Contains,
                    Value = "release",
                    IgnoreCase = true
                }
            }
        }
    },
    Pagination = new ChillPagination
    {
        Page = 1,
        PageResults = 25
    }
};

var results = new ChillEngine(context).Query(query);
```

The normal query stages still run:

1. automatic filtering
2. full-text search
3. ordering
4. pagination
5. `OnSelect(...)` for every returned entity

The inherited `Guid`, `FullTextSearch`, `Ordering`, `Pagination`, and `LightweightRequired` properties remain available.

## Apply To Any `IQueryable<T>`

`AutomaticQuery.ApplyTo(...)` also works independently of `ChillEngine` and does not require `T` to implement `IChillEntity`:

```csharp
var definition = new AutomaticQuery
{
    Filter = new AutomaticQueryGroup
    {
        Filters =
        {
            new AutomaticQueryFilter
            {
                PropertyName = nameof(ReportRow.Total),
                Operator = AutomaticQueryOperator.GreaterThanOrEqual,
                Value = 100
            }
        }
    }
};

IQueryable<ReportRow> filtered = definition.ApplyTo(reportRows);
```

`ApplyTo(...)` adds a `Where(...)` expression and does not enumerate the query.

## Operators

| Operator | Intended member type | Value |
| --- | --- | --- |
| `Equal`, `NotEqual` | scalar, nullable, enum, string, or `IChillEntity` reference | one value |
| `GreaterThan`, `GreaterThanOrEqual` | comparable CLR value | one value |
| `LessThan`, `LessThanOrEqual` | comparable CLR value | one value |
| `Between` | comparable CLR value | inclusive `Value` and `SecondValue` |
| `Contains` | string or collection | substring or collection item |
| `StartsWith`, `EndsWith` | string | one string value |
| `In` | scalar, enum, string, or `IChillEntity` reference | a collection of accepted values |
| `IsNull`, `IsNotNull` | nullable value or reference | none |
| `IsEmpty`, `IsNotEmpty` | string or collection | none |
| `Any`, `All` | collection | an `ItemFilter` group |

String equality, `Contains`, `StartsWith`, `EndsWith`, and `In` can normalize casing when `IgnoreCase` is `true`.

## CLR Values

Filter values are converted to the target property type before the expression is created. The prototype handles:

- strings and characters
- signed and unsigned numeric types
- `decimal`, `float`, and `double`
- `bool`
- enums by name or numeric value
- `Guid`
- `DateTime` and `DateTimeOffset`
- `DateOnly` and `TimeOnly`
- nullable variants
- `JsonElement` scalar values and arrays produced by JSON deserialization

Invalid conversions and unsupported operator/type combinations throw `ChillException` with a description of the invalid filter.

To keep public requests bounded, one definition can contain at most 100 filters, group nesting is limited to 8 levels, and a property path can contain at most 512 characters. Cyclic or null group structures are rejected before expression generation.

## Nested Property Paths

Use dot-separated paths for related or nested values:

```csharp
new AutomaticQueryFilter
{
    PropertyName = "Blog.Title",
    Operator = AutomaticQueryOperator.StartsWith,
    Value = "engineering",
    IgnoreCase = true
}
```

Property lookup is case-insensitive. Intermediate nullable references are guarded in the generated expression. For example, `Blog.Title IsNull` also matches an entity whose `Blog` reference is `null`.

An invalid path fails before query enumeration instead of silently ignoring the filter.

## `ChillEntity` References

Equality and membership filters on an `IChillEntity` reference compare its `Guid`. The filter value can be either the related entity or its GUID:

```csharp
new AutomaticQueryFilter
{
    PropertyName = nameof(Post.Blog),
    Operator = AutomaticQueryOperator.Equal,
    Value = selectedBlog.Guid
}
```

This avoids relying on CLR reference equality or attaching a detached entity solely to create a filter.

## Collections

Use `Contains` for a collection of scalar values:

```csharp
new AutomaticQueryFilter
{
    PropertyName = nameof(Article.Tags),
    Operator = AutomaticQueryOperator.Contains,
    Value = "release"
}
```

For a collection of `IChillEntity`, `Contains` compares the item GUID.

Use `Any` or `All` with `ItemFilter` when collection items need their own predicate. Paths inside `ItemFilter` are relative to each item:

```csharp
new AutomaticQueryFilter
{
    PropertyName = nameof(Blog.Posts),
    Operator = AutomaticQueryOperator.Any,
    ItemFilter = new AutomaticQueryGroup
    {
        Filters =
        {
            new AutomaticQueryFilter
            {
                PropertyName = nameof(Post.Title),
                Operator = AutomaticQueryOperator.Contains,
                Value = "release",
                IgnoreCase = true
            }
        }
    }
}
```

Use `IsEmpty` and `IsNotEmpty` when only collection presence matters.

## Logical Groups

Filters and nested groups within one `AutomaticQueryGroup` use the group's `LogicalOperator`:

```csharp
var root = new AutomaticQueryGroup
{
    LogicalOperator = AutomaticQueryLogicalOperator.And,
    Filters =
    {
        new AutomaticQueryFilter
        {
            PropertyName = nameof(Post.Author),
            Operator = AutomaticQueryOperator.Equal,
            Value = "Andrea"
        }
    },
    Groups =
    {
        new AutomaticQueryGroup
        {
            LogicalOperator = AutomaticQueryLogicalOperator.Or,
            Filters =
            {
                new AutomaticQueryFilter
                {
                    PropertyName = nameof(Post.Title),
                    Operator = AutomaticQueryOperator.Contains,
                    Value = "release"
                },
                new AutomaticQueryFilter
                {
                    PropertyName = nameof(Post.Title),
                    Operator = AutomaticQueryOperator.Contains,
                    Value = "roadmap"
                }
            }
        }
    }
};
```

This represents `Author == "Andrea" AND (Title contains "release" OR Title contains "roadmap")`.

## Compatibility And Extension

Automatic queries are additive. Continue using a custom `ChillQuery` when filtering requires:

- authorization or tenant rules tied to the current context
- provider-specific database functions
- joins, projections, or calculated expressions
- domain-specific behavior that cannot be expressed safely as a property filter

The prototype builds expression trees and leaves execution to the source query provider. Provider support can differ, especially for string normalization, date/time types, and nested collection operations. Verify important definitions against the production relational provider rather than relying only on in-memory behavior.

Planned integration work includes schema metadata for supported operators, UI filter builders, and relational-provider coverage.
