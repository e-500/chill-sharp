# Checking EF Core Reference Presence Without Loading

Versione italiana: [Italiano](./it/ReferenceExistence.md)

`ChillSharp.EF.ChillEntryExtension.Exist()` answers a small but useful question: does this reference navigation currently have all of its configured foreign-key values?

```csharp
using ChillSharp.EF;

var hasCustomerReference = context.Entry(order)
    .Reference(x => x.Customer)
    .Exist();
```

The call reads the current values of the FK properties from EF Core's change tracker. With the default argument, it does not query the database and does not load `order.Customer`.

> The extension is named `Exist`, not `Exists`. Its signature is `Exist(bool loadIfExist = false)`.

## When To Use It

Use `Exist()` in model logic that needs to distinguish an absent optional relationship from a relationship that has been assigned, while avoiding an unnecessary load of the principal entity. It is particularly useful in `OnUpdate`, `OnSelect`, or DTO-processing logic where only the branching decision is needed.

```csharp
public override void OnUpdate(IChillContext context)
{
    var db = (AppDbContext)context;

    if (db.Entry(this).Reference(x => x.Customer).Exist())
    {
        // A customer FK value has been assigned. Customer is still not loaded.
        CustomerSummaryRequired = true;
    }
    else
    {
        CustomerSummaryRequired = false;
    }
}
```

This is preferable to inspecting `Customer != null` when the navigation may simply be unloaded. A null navigation does not distinguish “no relationship” from “relationship not loaded.”

## Optional Loading

Pass `true` only when the next operation actually needs the related entity:

```csharp
var customerReference = context.Entry(order).Reference(x => x.Customer);

if (customerReference.Exist(loadIfExist: true) && order.Customer is { } customer)
{
    // EF Core loaded Customer when it was not already loaded.
    var customerName = customer.Name;
}
```

The behavior is:

| Call | FK values are incomplete or null | FK values are present and navigation is unloaded | Navigation loaded? |
| --- | --- | --- | --- |
| `Exist()` | Returns `false` | Returns `true` | No new load |
| `Exist(true)` | Returns `false` | Returns `true` | Attempts to load the reference |

If the navigation was already loaded, neither form loads it again. Because the result still describes FK values, `Exist(true)` can return `true` while the loaded navigation is null when an FK-less database contains an orphaned value.

## FK-Less Database Does Not Mean Relationship-Less Model

This extension works with a legacy or FK-less database implementation only if EF Core still knows the relationship and its dependent FK properties. A physical database constraint and EF Core relationship metadata are separate concerns.

For example, the database may not enforce a constraint from `Order.CustomerGuid` to `Customer.Guid`, but the EF model still needs the scalar FK and relationship mapping:

```csharp
public sealed class Order : ChillEntity
{
    public Guid? CustomerGuid { get; set; }
    public Customer? Customer { get; set; }
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .HasOne(x => x.Customer)
        .WithMany()
        .HasForeignKey(x => x.CustomerGuid);
}
```

`Exist()` reads `CustomerGuid` through this mapping. A relationship that is not configured in EF Core has no `ReferenceEntry` FK metadata for the extension to inspect; in that case, use the scalar key directly or configure the relationship.

Shadow FK properties are supported as long as EF Core has mapped the navigation. The extension obtains the FK property metadata from the navigation, rather than requiring a public CLR FK property.

## What The Result Means

`Exist()` is a local FK-value presence test. It does not issue an existence query for the principal row.

Consequently:

- `true` means every configured FK component is non-null in the tracked dependent entry.
- `false` means at least one FK component is null.
- `true` does not guarantee that the related row exists, especially when the database does not enforce FK constraints or contains legacy orphaned values.
- `true` does not mean the navigation is loaded.

If the business rule requires proof that a principal row exists, query for it explicitly, for example with `AnyAsync`, or use `Exist(true)` and then handle a null loaded navigation. Prefer the explicit query when you need a server-side existence check without materializing the principal.

```csharp
var customerRowExists = await context.Set<Customer>()
    .AnyAsync(x => x.Guid == order.CustomerGuid, cancellationToken);
```

## Composite Keys And Value Conventions

For a composite FK, `Exist()` returns `true` only when every component is non-null. A partially populated composite key returns `false`.

The extension checks for `null`; it does not validate sentinel values. For example, `Guid.Empty`, `0`, or an empty string is non-null and can therefore produce `true` if that is the value currently stored. Use validation appropriate to the domain when those values mean “unassigned.”

## Preconditions And Failure Modes

The entity must be attached to the same EF Core `DbContext` used to obtain its entry. Call the extension on the dependent-side reference navigation: the entity in the `ReferenceEntry` must own the FK properties that EF Core reports for that navigation. Calling it for a non-reference member, a navigation with no usable FK relationship metadata, or the principal-side navigation of a one-to-one relationship can throw `InvalidOperationException` because those FK properties do not belong to the inspected entry.

Do not use it for collection navigations. A collection needs a different question—whether at least one related row exists—which normally requires a database query.

## Decision Guide

| Need | Use |
| --- | --- |
| Determine whether an optional reference has assigned FK values; do not load it | `Reference(...).Exist()` |
| Load the reference only if FK values are assigned | `Reference(...).Exist(true)` |
| Prove the principal row exists | An explicit `Any`/`AnyAsync` query |
| Determine whether a collection has members | A query against the dependent set |

Keep `Exist()` for the narrow FK-presence decision. Its value is that it makes that intent explicit and avoids a related-entity load when the load is unnecessary.
