namespace ChillSharp.Client.Dto
{
    /// <summary>
    /// Lightweight descriptor for a registered Chill entity or query type.
    /// </summary>
    public class ChillDtoSchemaListItem
    {
        /// <summary>
        /// Localized label resolved from the Chill metadata.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Normalized Chill type name exposed by the API.
        /// </summary>
        public string ChillType { get; set; } = string.Empty;

        /// <summary>
        /// Descriptor kind: <c>entity</c> or <c>query</c>.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Related normalized Chill entity type.
        /// </summary>
        public string? RelatedChillType { get; set; }
    }
}
