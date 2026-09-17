namespace ChillSharp.Schema.Contracts;

/// <summary>
/// Seed label metadata for a schema relation entry.
/// </summary>
public class ChillDtoSchemaRelationLabel : IChillDtoSchemaRelationLabel
{
    public Guid? LabelGuid { get; set; }

    public string PrimaryDefaultText { get; set; } = string.Empty;

    public string SecondaryDefaultText { get; set; } = string.Empty;
}
