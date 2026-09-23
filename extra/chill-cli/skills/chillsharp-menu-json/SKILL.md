---
name: chillsharp-menu-json
description: Configure or repair ChillSharp workspace menu JSON for CRUD tasks, especially ChillType and ChillQuery values.
---

# ChillSharp Menu JSON Configuration

Use this skill when creating or correcting a persisted workspace menu item whose `ComponentName` is `crud` and whose `ComponentConfigurationJson` selects a ChillSharp entity and query.

## Resolve logical ChillTypes from C# namespaces

`chillType` and `chillQuery` are logical ChillTypes, not filesystem paths. Derive each value from the declaring C# namespace after removing the application's `IChillContext.GetChillTypePrefix()`.

For example, with a prefix of `MyApp` and these declarations:

```csharp
namespace MyApp.Model;
public class Item : ChillEntity { }
public class ItemQuery : ChillQuery { }
```

use `Model.Item` and `Model.ItemQuery`—not `Query.ItemQuery`, even if `ItemQuery.cs` resides in a `Model/Query` folder.

## CRUD configuration

Keep the entity and query aligned, retain any intentional relations, and use valid JSON:

```json
{
  "chillType": "Model.Item",
  "viewCode": "default",
  "chillQuery": "Model.ItemQuery",
  "relations": []
}
```

When repairing an existing menu item, change only the incorrect logical type unless the user requests other configuration changes. Save the menu entry, reopen the task, and confirm that the type-resolution error is gone.
