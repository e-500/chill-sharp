/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core 
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Runtime.CompilerServices; // Provides CallerMemberName, used to capture the name of the calling member automatically
using System.Globalization;
using ChillSharp.Dto;

namespace ChillSharp.Annotations
{
    public enum ChillPropertyOptionalBoolean
    {
        Unspecified = 0,
        False = 1,
        True = 2
    }

    /// <summary>
    /// An attribute used to mark a class or property as a "Chill Entity" field.
    /// <para>It can store metadata about the field's type and nullability.</para>
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the terms of the 
    /// GNU Affero General Public License as published by the Free Software Foundation, 
    /// either version 3 of the License, or (at your option) any later version.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ChillPropertyAttribute : Attribute
    {
        /// <summary>
        /// The constructor for ChillPropertyAttribute.
        /// The optional CallerMemberName attribute automatically supplies the name of the member
        /// (e.g., a property or method) to which this attribute is applied, unless explicitly provided.
        /// </summary>
        /// <param name="PropertyName">
        /// The name of the property this attribute is applied to.
        /// Automatically filled in by the compiler when not manually provided.
        /// </param>
        /// <param name="CallOnInflate">
        /// When <see langword="true"/>, ChillSharp calls <c>OnInflate()</c> so the application can populate this property manually.
        /// Use it for calculated, not-mapped, lazily loaded, or collection properties that Entity Framework cannot hydrate directly.
        /// </param>
        /// <param name="IsNullable">
        /// Optional nullability override for schema generation. Use <see cref="ChillPropertyOptionalBoolean.True"/> or
        /// <see cref="ChillPropertyOptionalBoolean.False"/> when reflection/nullability attributes are not enough.
        /// </param>
        /// <param name="IsReadOnly">
        /// Optional read-only override for schema generation. Use it to tell clients whether this property should be edited.
        /// </param>
        /// <param name="MinLength">
        /// Minimum allowed length for string or text values. Use <c>-1</c> to leave it unspecified.
        /// </param>
        /// <param name="MaxLength">
        /// Maximum allowed length for string or text values. Use <c>-1</c> to leave it unspecified.
        /// </param>
        /// <param name="IntegerMinValue">
        /// Minimum allowed value for integer-like properties. Use <see cref="long.MinValue"/> to leave it unspecified.
        /// </param>
        /// <param name="IntegerMaxValue">
        /// Maximum allowed value for integer-like properties. Use <see cref="long.MinValue"/> to leave it unspecified.
        /// </param>
        /// <param name="DecimalMinValue">
        /// Minimum allowed value for decimal-like properties. Use <see cref="double.NaN"/> to leave it unspecified.
        /// </param>
        /// <param name="DecimalMaxValue">
        /// Maximum allowed value for decimal-like properties. Use <see cref="double.NaN"/> to leave it unspecified.
        /// </param>
        /// <param name="DecimalPlaces">
        /// Preferred number of decimal places for UI and schema consumers. Use <c>-1</c> to leave it unspecified.
        /// </param>
        /// <param name="Precision">
        /// Total precision for decimal-like values. Use <c>-1</c> to leave it unspecified.
        /// </param>
        /// <param name="Scale">
        /// Decimal scale for decimal-like values. Use <c>-1</c> to leave it unspecified.
        /// </param>
        /// <param name="DateFormat">
        /// Preferred date, time, or datetime format hint exposed through the schema.
        /// </param>
        /// <param name="RegexPattern">
        /// Regular expression pattern hint for validating or rendering text values.
        /// </param>
        /// <param name="CustomFormat">
        /// Custom semantic format hint for clients, for example <c>json</c>, <c>email</c>, <c>url</c>, or an application-specific value.
        /// </param>
        /// <param name="EnumValues">
        /// Optional ordered list of allowed or suggested string values to expose in schema metadata.
        /// </param>
        public ChillPropertyAttribute(
            [CallerMemberName] string? PropertyName = null, 
            bool CallOnInflate = false,
            ChillPropertyOptionalBoolean IsNullable = ChillPropertyOptionalBoolean.Unspecified,
            ChillPropertyOptionalBoolean IsReadOnly = ChillPropertyOptionalBoolean.Unspecified,
            int MinLength = -1,
            int MaxLength = -1,
            long IntegerMinValue = long.MinValue,
            long IntegerMaxValue = long.MinValue,
            double DecimalMinValue = double.NaN,
            double DecimalMaxValue = double.NaN,
            int DecimalPlaces = -1,
            int Precision = -1,
            int Scale = -1,
            string? DateFormat = null,
            string? RegexPattern = null,
            string? CustomFormat = null,
            params string[] EnumValues)
        {
            this.PropertyName = PropertyName;
            this.CallOnInflate = CallOnInflate;
            this.IsNullable = NormalizeOptionalBoolean(IsNullable);
            this.IsReadOnly = NormalizeOptionalBoolean(IsReadOnly);
            this.MinLength = NormalizeOptionalInt(MinLength);
            this.MaxLength = NormalizeOptionalInt(MaxLength);
            this.IntegerMinValue = NormalizeOptionalLong(IntegerMinValue);
            this.IntegerMaxValue = NormalizeOptionalLong(IntegerMaxValue);
            this.DecimalMinValue = NormalizeOptionalDouble(DecimalMinValue);
            this.DecimalMaxValue = NormalizeOptionalDouble(DecimalMaxValue);
            this.DecimalPlaces = NormalizeOptionalInt(DecimalPlaces);
            this.Precision = NormalizeOptionalInt(Precision);
            this.Scale = NormalizeOptionalInt(Scale);
            this.DateFormat = DateFormat;
            this.RegexPattern = RegexPattern;
            this.CustomFormat = CustomFormat;
            this.EnumValues = NormalizeOptionalStrings(EnumValues);
        }

        /// <summary>
        /// The constructor for ChillPropertyAttribute WITH LABEL TEXTS
        /// The optional CallerMemberName attribute automatically supplies the name of the member
        /// (e.g., a property or method) to which this attribute is applied, unless explicitly provided.
        /// </summary>
        /// <param name="UniquePropertyKeyString">
        /// Stable GUID string used as the translation key for this property label.
        /// </param>
        /// <param name="PrimaryLanguageLabel">
        /// Primary language label text, usually international English.
        /// </param>
        /// <param name="SecondaryLanguageLabel">
        /// Secondary language label text, usually the developer or software-house language.
        /// </param>
        /// <param name="PropertyName">
        /// The name of the property this attribute is applied to.
        /// Automatically filled in by the compiler when not manually provided.
        /// </param>
        /// <param name="CallOnInflate">
        /// When <see langword="true"/>, ChillSharp calls <c>OnInflate()</c> so the application can populate this property manually.
        /// Use it for calculated, not-mapped, lazily loaded, or collection properties that Entity Framework cannot hydrate directly.
        /// </param>
        /// <param name="IsNullable">
        /// Optional nullability override for schema generation. Use <see cref="ChillPropertyOptionalBoolean.True"/> or
        /// <see cref="ChillPropertyOptionalBoolean.False"/> when reflection/nullability attributes are not enough.
        /// </param>
        /// <param name="IsReadOnly">
        /// Optional read-only override for schema generation. Use it to tell clients whether this property should be edited.
        /// </param>
        /// <param name="MinLength">
        /// Minimum allowed length for string or text values. Use <c>-1</c> to leave it unspecified.
        /// </param>
        /// <param name="MaxLength">
        /// Maximum allowed length for string or text values. Use <c>-1</c> to leave it unspecified.
        /// </param>
        /// <param name="IntegerMinValue">
        /// Minimum allowed value for integer-like properties. Use <see cref="long.MinValue"/> to leave it unspecified.
        /// </param>
        /// <param name="IntegerMaxValue">
        /// Maximum allowed value for integer-like properties. Use <see cref="long.MinValue"/> to leave it unspecified.
        /// </param>
        /// <param name="DecimalMinValue">
        /// Minimum allowed value for decimal-like properties. Use <see cref="double.NaN"/> to leave it unspecified.
        /// </param>
        /// <param name="DecimalMaxValue">
        /// Maximum allowed value for decimal-like properties. Use <see cref="double.NaN"/> to leave it unspecified.
        /// </param>
        /// <param name="DecimalPlaces">
        /// Preferred number of decimal places for UI and schema consumers. Use <c>-1</c> to leave it unspecified.
        /// </param>
        /// <param name="Precision">
        /// Total precision for decimal-like values. Use <c>-1</c> to leave it unspecified.
        /// </param>
        /// <param name="Scale">
        /// Decimal scale for decimal-like values. Use <c>-1</c> to leave it unspecified.
        /// </param>
        /// <param name="DateFormat">
        /// Preferred date, time, or datetime format hint exposed through the schema.
        /// </param>
        /// <param name="RegexPattern">
        /// Regular expression pattern hint for validating or rendering text values.
        /// </param>
        /// <param name="CustomFormat">
        /// Custom semantic format hint for clients, for example <c>json</c>, <c>email</c>, <c>url</c>, or an application-specific value.
        /// </param>
        /// <param name="EnumValues">
        /// Optional ordered list of allowed or suggested string values to expose in schema metadata.
        /// </param>
        public ChillPropertyAttribute(
            string UniquePropertyKeyString,
            string PrimaryLanguageLabel,
            string SecondaryLanguageLabel,
            [CallerMemberName] string? PropertyName = null,
            bool CallOnInflate = false,
            ChillPropertyOptionalBoolean IsNullable = ChillPropertyOptionalBoolean.Unspecified,
            ChillPropertyOptionalBoolean IsReadOnly = ChillPropertyOptionalBoolean.Unspecified,
            int MinLength = -1,
            int MaxLength = -1,
            long IntegerMinValue = long.MinValue,
            long IntegerMaxValue = long.MinValue,
            double DecimalMinValue = double.NaN,
            double DecimalMaxValue = double.NaN,
            int DecimalPlaces = -1,
            int Precision = -1,
            int Scale = -1,
            string? DateFormat = null,
            string? RegexPattern = null,
            string? CustomFormat = null,
            params string[] EnumValues)
        {
            this.PropertyName = PropertyName;
            this.CallOnInflate = CallOnInflate;
            this.UniquePropertyKey = new Guid(UniquePropertyKeyString);
            this.PrimaryLanguageLabel = PrimaryLanguageLabel;
            this.SecondaryLanguageLabel = SecondaryLanguageLabel;
            this.IsNullable = NormalizeOptionalBoolean(IsNullable);
            this.IsReadOnly = NormalizeOptionalBoolean(IsReadOnly);
            this.MinLength = NormalizeOptionalInt(MinLength);
            this.MaxLength = NormalizeOptionalInt(MaxLength);
            this.IntegerMinValue = NormalizeOptionalLong(IntegerMinValue);
            this.IntegerMaxValue = NormalizeOptionalLong(IntegerMaxValue);
            this.DecimalMinValue = NormalizeOptionalDouble(DecimalMinValue);
            this.DecimalMaxValue = NormalizeOptionalDouble(DecimalMaxValue);
            this.DecimalPlaces = NormalizeOptionalInt(DecimalPlaces);
            this.Precision = NormalizeOptionalInt(Precision);
            this.Scale = NormalizeOptionalInt(Scale);
            this.DateFormat = DateFormat;
            this.RegexPattern = RegexPattern;
            this.CustomFormat = CustomFormat;
            this.EnumValues = NormalizeOptionalStrings(EnumValues);
        }

        /// <summary>
        /// Holds the name of the field or property associated with this attribute.
        /// This is kept private, but could be used internally if reflection is applied.
        /// </summary>
        public string? PropertyName { get; set; }

        /// <summary>
        /// Tells ChillSharp engine to call OnInflate() to populate the property
        /// Generally because EF can't do it automatically (eg. NotMapped)
        /// </summary>
        public bool CallOnInflate { get; set; }

        /// <summary>
        /// Unique key for the property to store the label for translation purposes
        /// </summary>
        public Guid? UniquePropertyKey { get; set; }

        /// <summary>
        /// Primary language label text (International english)
        /// </summary>
        public string? PrimaryLanguageLabel { get; set; }

        /// <summary>
        /// Secondary language label text (Software house / Developer language)
        /// </summary>
        public string? SecondaryLanguageLabel { get ; set; }

        /// <summary>
        /// Description exposed to MCP clients for the property.
        /// </summary>
        public string? MCPDescription { get; set; }

        /// <summary>
        /// Optional Chill query type used by clients to perform lookups for this reference property.
        /// </summary>
        public string? ReferenceChillTypeQuery { get; set; }

        /// <summary>
        /// Optional frontend-oriented field type override for schema generation.
        /// Use <see cref="ChillDtoPropertyType.Unknown"/> to leave the CLR type mapping unchanged.
        /// </summary>
        public ChillDtoPropertyType PropertyType { get; set; } = ChillDtoPropertyType.Unknown;

        /// <summary>
        /// Explicit nullable flag used by schema generation when provided.
        /// </summary>
        public bool? IsNullable { get; set; }

        /// <summary>
        /// Explicit read-only flag used by schema generation when provided.
        /// </summary>
        public bool? IsReadOnly { get; set; }

        /// <summary>
        /// Minimum string length allowed for textual values.
        /// </summary>
        public int? MinLength { get; set; }

        /// <summary>
        /// Maximum string length allowed for textual values.
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Minimum allowed value for integer-like properties.
        /// </summary>
        public long? IntegerMinValue { get; set; }

        /// <summary>
        /// Maximum allowed value for integer-like properties.
        /// </summary>
        public long? IntegerMaxValue { get; set; }

        /// <summary>
        /// Minimum allowed value for decimal-like properties.
        /// </summary>
        public double? DecimalMinValue { get; set; }

        /// <summary>
        /// Maximum allowed value for decimal-like properties.
        /// </summary>
        public double? DecimalMaxValue { get; set; }

        /// <summary>
        /// Preferred number of decimal places for decimal values.
        /// </summary>
        public int? DecimalPlaces { get; set; }

        /// <summary>
        /// Total precision for decimal-like values.
        /// </summary>
        public int? Precision { get; set; }

        /// <summary>
        /// Decimal scale for decimal-like values.
        /// </summary>
        public int? Scale { get; set; }

        /// <summary>
        /// Preferred date or time format string.
        /// </summary>
        public string? DateFormat { get; set; }

        /// <summary>
        /// Regular-expression hint for text validation.
        /// </summary>
        public string? RegexPattern { get; set; }

        /// <summary>
        /// Generic custom format hint for UI consumers.
        /// </summary>
        public string? CustomFormat { get; set; }

        /// <summary>
        /// Ordered enum-like values to expose in schema metadata.
        /// </summary>
        public string[]? EnumValues { get; set; }

        /// <summary>
        /// Lookup query values, example { "OptionalSearchProperty": "@{OtherField}", "MandatorySearchProperty": "${AlternativeField}" }
        /// - true for boolean
        /// - 1 for numbers
        /// - "hello" for strings
        /// - "@{FieldName}" optional value
        /// - "${FieldName}" to get value from the same entity properrties
        /// </summary>
        public string? LookupQueryValues { get; set; }

        /// <summary>
        /// Arbitrary metadata entries encoded as <c>key=value</c>.
        /// </summary>
        public string[]? MetadataEntries { get; set; }

        private static int? NormalizeOptionalInt(int value)
        {
            return value >= 0 ? value : null;
        }

        private static bool? NormalizeOptionalBoolean(ChillPropertyOptionalBoolean value)
        {
            return value switch
            {
                ChillPropertyOptionalBoolean.True => true,
                ChillPropertyOptionalBoolean.False => false,
                _ => null
            };
        }

        private static long? NormalizeOptionalLong(long value)
        {
            return value != long.MinValue ? value : null;
        }

        private static double? NormalizeOptionalDouble(double value)
        {
            return double.IsNaN(value) ? null : value;
        }

        private static string[]? NormalizeOptionalStrings(string[]? values)
        {
            return values is { Length: > 0 } ? values : null;
        }

        /// <summary>
        /// Converts <see cref="MetadataEntries"/> into a dictionary suitable for DTO schema emission.
        /// </summary>
        public Dictionary<string, string> GetMetadata()
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (MetadataEntries == null)
            {
                return metadata;
            }

            foreach (var entry in MetadataEntries)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    continue;
                }

                var separatorIndex = entry.IndexOf('=');
                if (separatorIndex <= 0 || separatorIndex == entry.Length - 1)
                {
                    metadata[entry.Trim()] = string.Empty;
                    continue;
                }

                var key = entry[..separatorIndex].Trim();
                var value = entry[(separatorIndex + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    metadata[key] = value;
                }
            }

            return metadata;
        }

        /// <summary>
        /// Returns the minimum decimal value as <see cref="decimal"/> when configured.
        /// </summary>
        public decimal? GetDecimalMinValue()
        {
            return DecimalMinValue.HasValue
                ? Convert.ToDecimal(DecimalMinValue.Value, CultureInfo.InvariantCulture)
                : null;
        }

        /// <summary>
        /// Returns the maximum decimal value as <see cref="decimal"/> when configured.
        /// </summary>
        public decimal? GetDecimalMaxValue()
        {
            return DecimalMaxValue.HasValue
                ? Convert.ToDecimal(DecimalMaxValue.Value, CultureInfo.InvariantCulture)
                : null;
        }
    }
}
