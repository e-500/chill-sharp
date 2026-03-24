using ChillSharp.Annotations;
using ChillSharp.EF;
using System.Reflection;

namespace ChillSharp.Dto
{
    /// <summary>
    /// Lightweight descriptor for a registered Chill entity or query type.
    /// </summary>
    public class ChillDtoSchemaListItem
    {
        /// <summary>
        /// Localized label resolved from the Chill entity attribute.
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
        /// For entities this matches <see cref="ChillType"/>, for queries it is the concrete target entity when available.
        /// </summary>
        public string? RelatedChillType { get; set; }

        public static ChillDtoSchemaListItem FromEntityType(Type entityType, string shrinkTypePrefix, IChillContext? context, string? cultureName)
        {
            var chillAttr = entityType.GetCustomAttribute<ChillEntityAttribute>(inherit: true);

            return new ChillDtoSchemaListItem
            {
                Name = ChillLabelResolver.Resolve(
                    chillAttr?.PrimaryLanguageLabel,
                    chillAttr?.SecondaryLanguageLabel,
                    entityType.Name,
                    context,
                    cultureName),
                ChillType = NormalizeChillType(entityType, shrinkTypePrefix),
                Type = "entity",
                RelatedChillType = NormalizeChillType(entityType, shrinkTypePrefix)
            };
        }

        public static ChillDtoSchemaListItem FromQueryType(Type queryType, string shrinkTypePrefix, IChillContext? context, string? cultureName)
        {
            var chillAttr = queryType.GetCustomAttribute<ChillEntityAttribute>(inherit: true);

            return new ChillDtoSchemaListItem
            {
                Name = ChillLabelResolver.Resolve(
                    chillAttr?.PrimaryLanguageLabel,
                    chillAttr?.SecondaryLanguageLabel,
                    queryType.Name,
                    context,
                    cultureName),
                ChillType = NormalizeChillType(queryType, shrinkTypePrefix),
                Type = "query",
                RelatedChillType = ResolveQueryRelatedChillType(queryType, shrinkTypePrefix)
            };
        }

        internal static string NormalizeChillType(Type type, string shrinkTypePrefix)
        {
            return ChillTypeResolver.NormalizeChillType(type, shrinkTypePrefix);
        }

        private static string? ResolveQueryRelatedChillType(Type queryType, string shrinkTypePrefix)
        {
            var entityType = ChillQueryTypeResolver.ResolveRelatedEntityType(queryType);

            if (entityType == null)
            {
                return null;
            }

            return NormalizeChillType(entityType, shrinkTypePrefix);
        }
    }
}
