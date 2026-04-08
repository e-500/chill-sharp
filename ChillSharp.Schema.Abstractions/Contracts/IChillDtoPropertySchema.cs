using ChillSharp.Dto;

namespace ChillSharp.Schema.Contracts;

/// <summary>
/// Read-only contract for DTO property schema metadata shared with ChillSharp core components.
/// </summary>
public interface IChillDtoPropertySchema
{
    ChillDtoPropertyType PropertyType { get; }
    string Name { get; }
    string DisplayName { get; }
    string MCPDescription { get; }
    bool? IsNullable { get; }
    bool? IsReadOnly { get; }
    int? MinLength { get; }
    int? MaxLength { get; }
    long? IntegerMinValue { get; }
    long? IntegerMaxValue { get; }
    decimal? DecimalMinValue { get; }
    decimal? DecimalMaxValue { get; }
    int? DecimalPlaces { get; }
    int? Precision { get; }
    int? Scale { get; }
    string DateFormat { get; }
    string ReferenceChillType { get; }
    string ReferenceChillTypeQuery { get; }
    IReadOnlyList<string> EnumValues { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
    string CustomFormat { get; }
    string RegexPattern { get; }
}
