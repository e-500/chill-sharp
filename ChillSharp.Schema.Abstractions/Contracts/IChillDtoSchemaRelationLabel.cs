namespace ChillSharp.Schema.Contracts;

/// <summary>
/// Seed label metadata exposed for UI relation rendering and translation lookup.
/// </summary>
public interface IChillDtoSchemaRelationLabel
{
    Guid? LabelGuid { get; }
    string PrimaryDefaultText { get; }
    string SecondaryDefaultText { get; }
}
