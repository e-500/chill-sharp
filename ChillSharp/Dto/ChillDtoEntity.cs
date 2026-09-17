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

using ChillSharp.Annotations;
using ChillSharp.EF;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Dto
{
    /// <summary>
    /// Represents a lightweight web-friendly version of an EF Core entity.
    /// This class omits collection properties (navigation lists, etc.) for simplicity
    /// and is intended for serialization or transmission via web APIs.
    /// 
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or removal must comply with GPLv3 licensing terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// ©️2025 Andrea Piovesan</para>
    /// </summary>
    public class ChillDtoEntity : IDtoChillable
    {
        public ChillDtoEntity() { }

        public ChillDtoEntity(IChillContext Context, IChillEntity Entity, List<ChillDtoProperty>? RequiredProperties = null)
        {
            Guid = Entity.Guid;
            ChillType = _TestEntityAndGetChillType(Context, Entity);
            Label = Entity.Label;
            ShortLabel = Entity.ShortLabel;
            FromEntity(Context, Entity, RequiredProperties);
        }

        /// <summary>
        /// Globally unique identifier of the object.
        /// This corresponds to the entity's primary key in EF Core.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Optional string identifying the entity type or category.
        /// Useful for polymorphic handling of different entity types in a generic web model.
        /// </summary>
        public string ChillType { get; set; } = string.Empty;

        /// <summary>
        /// A human-readable label or name for the object.
        /// Commonly used for displaying the entity in UI lists or dropdowns.
        /// </summary>
        public string? Label { get; set; } = null;

        /// <summary>
        /// An abbreviated version of the label, if applicable.
        /// Often used in compact UI representations or summaries.
        /// </summary>
        public string? ShortLabel { get; set; } = null;

        /// <summary>
        /// A dictionary mapping field names (property keys) to their corresponding values.
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();

        #region HELPERS

        public ChillDtoEntity Mock()
        {
            var mock = new ChillDtoEntity();
            mock.Guid = Guid;
            mock.ChillType = ChillType;
            mock.Label = Label;
            mock.ShortLabel = ShortLabel;
            return mock;
        }

        /// <summary>
        /// COMMENT: Test if the Entity has the correct TypeIdPrefix and return the short form
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="Entity"></param>
        /// <returns></returns>
        /// <exception cref="ChillException"></exception>
        private string _TestEntityAndGetChillType(IChillContext Context, IChillEntity Entity)
        {
            var chillType = Entity.GetType().FullName;
            var chillTypePrefix = Context.GetChillTypePrefix();
            if (string.IsNullOrEmpty(chillType))
                throw new ChillException($"Entity type full name ({chillType}) is invalid");
            if (!chillType.StartsWith(chillTypePrefix))
                throw new ChillException($"Entity type full name ({chillType}) doesn't start with {chillTypePrefix}");

            return chillType.Substring(chillTypePrefix.Length + 1);
        }

        /// <summary>
        /// Initializes this DTO entity from an existing Chill entity object.
        /// Extracts annotated properties and their values, storing them in the <see cref="Fields"/> collection.
        /// </summary>
        /// <param name="Context">The Chill context providing type information.</param>
        /// <param name="Entity">The entity object to serialize into this DTO.</param>
        public void FromEntity(IChillContext Context, IChillEntity Entity, List<ChillDtoProperty>? RequiredProperties = null)
        {
            // Test and get main fields from chill entity
            ChillType = _TestEntityAndGetChillType(Context, Entity);
            Guid = Entity.Guid;
            Label = Entity.Label;
            ShortLabel = Entity.ShortLabel;

            // All chill properties matching the list
            // or all chill properties if list is null
            // No fields if list is empty.
            var ef_props = Entity.GetType().GetProperties().Where(prop =>
                prop.IsDefined(typeof(ChillPropertyAttribute), false) &&
                (RequiredProperties == null || RequiredProperties.Any(x => x.PropertyName == prop.Name)));

            Properties = ChillDtoObjectMapper.BuildProperties(
                Context,
                Entity,
                ChillType,
                ef_props,
                propertyName => RequiredProperties?
                    .FirstOrDefault(x => x.PropertyName == propertyName)?
                    .SubProperties ?? [],
                propertyName => Entity.OnInflate(Context, propertyName));
        }

        /// <summary>
        /// Applies values from this DTO entity to a Chill entity instance.
        /// The receiving object must have matching field names annotated with <see cref="ChillPropertyAttribute"/>.
        /// </summary>
        /// <param name="Context">The current Chill context for validation and mapping.</param>
        /// <param name="Entity">The target entity instance to populate with DTO data.</param>
        /// <exception cref="ChillException">Thrown if type validation or property assignment fails.</exception>
        public void ToEntity(IChillContext Context, IChillEntity Entity)
        {
            // Test only if Entity is a valid chill entity
            string EntityChillType = _TestEntityAndGetChillType(Context, Entity);
            if (ChillType != EntityChillType)
                throw new ChillException($"Entity ChillType ({EntityChillType}) differs from Dto ChillType ({ChillType})");
            Entity.Guid = Guid;
            Entity.Label = Label ?? "";
            Entity.ShortLabel = ShortLabel ?? "";

            var dbx = (DbContext)Context;
            if (dbx == null)
                throw new ChillException("Can't cast to IChillContext to DbContext");

            var ef_props = Entity.GetType().GetProperties()
                .Where(prop => prop.IsDefined(typeof(ChillPropertyAttribute), false))
                .Where(x => Properties.Keys.Contains(x.Name));
            ChillDtoObjectMapper.ApplyProperties(
                Context,
                Entity,
                ChillType,
                Properties,
                ef_props,
                "entity",
                loadTrackedCollections: true,
                propertyName => Entity.OnInflate(Context, propertyName));
        }
        #endregion
    }
}
