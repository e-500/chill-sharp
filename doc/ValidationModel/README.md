# Validation Model

ChillSharp supports the standard ASP.NET Core / .NET validation attributes on entity and query properties, as long as those properties are also marked with `[ChillProperty]`.

## Standard validation on Chill properties

Decorate your `ChillEntity` or `ChillQuery` properties with both `[ChillProperty]` and the usual DataAnnotations attributes such as `[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]`, and so on.

```csharp
using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;

public class Customer : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [ChillProperty(
        UniquePropertyKeyString: "2F262D7E-F676-4857-B41D-D31C766AA38F",
        PrimaryLanguageLabel: "Name",
        SecondaryLanguageLabel: "Nome")]
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(80, ErrorMessage = "Customer name must be 80 characters or less.")]
    public string Name { get; set; } = string.Empty;
}
```

At runtime ChillSharp validates only the properties decorated with `[ChillProperty]`.

If a property has DataAnnotations attributes but is not a Chill property, ChillSharp ignores it in the Chill validation pipeline.

## Combining DataAnnotations with `OnValidation()`

You can still add custom ChillSharp validation by overriding `OnValidation()`.

```csharp
public override IEnumerable<ChillValidationError> OnValidation(IChillContext context)
{
    if (Name == "admin")
    {
        return
        [
            new ChillValidationError
            {
                FieldName = nameof(Name),
                Message = "The name 'admin' is reserved."
            }
        ];
    }

    return [];
}
```

Runtime behavior is:

1. ChillSharp runs standard DataAnnotations validation on `[ChillProperty]` members.
2. ChillSharp then runs your custom `OnValidation()` logic.
3. The returned errors are exposed as `ChillValidationError` using the human-readable DataAnnotations `ErrorMessage`.

## Update behavior

The same validation pipeline also runs automatically at the beginning of the internal post-update runtime hook used by ChillSharp.

This means:

- If the client calls validation explicitly, it receives `ChillValidationError` entries for annotation-based and custom validation errors.
- If the client skips explicit validation and goes directly to create or update, ChillSharp still validates the entity during the update lifecycle and throws a `ChillValidationException` with the same human-readable messages.

## Notes

- Use standard DataAnnotations for simple field rules.
- Use `OnValidation()` for rules that depend on multiple fields, business logic, or database lookups.
- To make a property part of ChillSharp validation, always add `[ChillProperty]`.

## Appendix: GUID-based validation messages

As an optional flexibility hack, a DataAnnotations `ErrorMessage` can contain a GUID string instead of final user text.

Then you can provide the actual primary and secondary texts by overriding `GetValidationMessageDefinitions()`.

```csharp
public class Customer : ChillEntity
{
    [ChillProperty(
        UniquePropertyKeyString: "B2AB35A8-6A89-4D39-8F1D-183F686811A9",
        PrimaryLanguageLabel: "Code",
        SecondaryLanguageLabel: "Codice")]
    [Required(ErrorMessage = "4F880CC1-5C7A-4E23-982A-5F0C490B44DE")]
    public string Code { get; set; } = string.Empty;

    public override IEnumerable<ChillValidationMessageDefinition> GetValidationMessageDefinitions(IChillContext context)
    {
        return
        [
            new ChillValidationMessageDefinition
            {
                MessageGuid = Guid.Parse("4F880CC1-5C7A-4E23-982A-5F0C490B44DE"),
                PrimaryLanguageMessage = "Code is required.",
                SecondaryLanguageMessage = "Il codice e obbligatorio."
            }
        ];
    }
}
```

When ChillSharp sees that GUID in `ErrorMessage`, it resolves the final message through the same primary/secondary language convention used elsewhere in ChillSharp metadata.

Notes:

- This is optional. Plain human-readable `ErrorMessage` values still work normally.
- Use this only when you want a stable identifier for a validation text.
- If the GUID is not found in `GetValidationMessageDefinitions()`, ChillSharp falls back to the raw `ErrorMessage` string.
