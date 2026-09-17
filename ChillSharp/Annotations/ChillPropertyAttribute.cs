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
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
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
        /// If set ChillSharp call OnInflate() asking to load the collection or the property in general
        /// </param>
        /// <param name="UniquePropertyKey">
        /// Unique key for the property to store the label for translation purposes
        /// </param>
        /// <param name="PrimaryLanguageLabel">
        /// Primary language label text (International english)
        /// </param>
        /// <param name="SecondaryLanguageLabel">
        /// Secondary language label text (Software house / Developer language)
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
        /// <param name="UniquePropertyKey">
        /// Unique key for the property to store the label for translation purposes
        /// </param>
        /// <param name="PrimaryLanguageLabel">
        /// Primary language label text (International english)
        /// </param>
        /// <param name="SecondaryLanguageLabel">
        /// Secondary language label text (Software house / Developer language)
        /// </param>
        /// <param name="PropertyName">
        /// The name of the property this attribute is applied to.
        /// Automatically filled in by the compiler when not manually provided.
        /// </param>
        /// <param name="CallOnInflate">
        /// If set ChillSharp call OnInflate() asking to load the collection or the property in general
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
