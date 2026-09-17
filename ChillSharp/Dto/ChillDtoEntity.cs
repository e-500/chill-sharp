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
using System.Collections;
using System.Text.Json;

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

            var dbx = (DbContext)Context;

            Properties = ef_props.ToDictionary(
                ef_prop => ef_prop.Name,
                ef_prop => {
                    var attr = (ChillPropertyAttribute)ef_prop.GetCustomAttributes(typeof(ChillPropertyAttribute), false).First();
                    var propertyName = ef_prop.Name; // TODO consent to override internal name using a custom property on ChillPropertyAttribute

                    if (attr.CallOnInflate)
                        Entity.OnInflate(Context, propertyName);

                    // CHILL-ENTITY
                    if (typeof(IChillEntity).IsAssignableFrom(ef_prop.PropertyType))
                    {
                        // if is a reference load it and convert to ChillDtoEntity
                        if (dbx.Entry(Entity).Reference(propertyName).Exist(true))
                        {
                            var ef_obj = (IChillEntity?)ef_prop.GetValue(Entity);
                            // NULL
                            if (ef_obj == null)
                                return null;

                            ChillDtoProperty? reqProp = null;
                            if (RequiredProperties != null)
                                reqProp = RequiredProperties.Where(x => x.PropertyName == propertyName).FirstOrDefault();
                            return new ChillDtoEntity(Context, ef_obj, (reqProp?.SubProperties ?? new List<ChillDtoProperty>()));
                        }
                        return null;
                    }
                    // CHILL-ENTITIES-COLLECTIONS
                    else if (typeof(IEnumerable<IChillEntity>).IsAssignableFrom(ef_prop.PropertyType))
                    {
                        // Try to load collection 
                        dbx.Entry(Entity).Collection(propertyName)?.Load();

                        var entity = (IEnumerable<IChillEntity>?)ef_prop.GetValue(Entity);
                        if (entity != null)
                        {
                            var ef_obj_coll = (IEnumerable<IChillEntity>?)ef_prop.GetValue(Entity);
                            // NULL
                            if (ef_obj_coll == null)
                                return null;

                            return ef_obj_coll.Select(ef_obj => {
                                ChillDtoProperty? reqProp = null;
                                if (RequiredProperties != null)
                                    reqProp = RequiredProperties.Where(x => x.PropertyName == propertyName).FirstOrDefault();
                                return new ChillDtoEntity(Context, ef_obj, (reqProp?.SubProperties ?? new List<ChillDtoProperty>()));
                            });                           
                        }
                        else
                            return null;
                    }
                    // OTHER PROPERTY TYPES
                    else 
                    {
                        return (object?)ef_prop.GetValue(Entity);
                    }
                });
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
            foreach (var ef_prop in ef_props)
            {
                var attr = (ChillPropertyAttribute)ef_prop.GetCustomAttributes(typeof(ChillPropertyAttribute), false).First();
                var propertyName = ef_prop.Name; // TODO consent to override internal name using a custom property on ChillPropertyAttribute
                var value = Properties[propertyName];

                if (attr.CallOnInflate)
                    Entity.OnInflate(Context, propertyName);

                try
                {
                    object? parsedValue = value;
                    if (value is JsonElement jsonElement)
                    {
                        var targetType = ef_prop.PropertyType;
                        // Handle nullable types
                        if (Nullable.GetUnderlyingType(targetType) is Type underlyingType)
                            targetType = underlyingType;

                        // NULL
                        if (jsonElement.ValueKind == JsonValueKind.Null)
                        {
                            parsedValue = null;
                            // Finalize
                            ef_prop.SetValue(Entity, parsedValue);
                        }
                        // CHILL-ENTITY
                        else if (typeof(IChillEntity).IsAssignableFrom(ef_prop.PropertyType))
                        {
                            // Get the incoming object
                            var incomingChillEntity = JsonSerializer.Deserialize<ChillDtoEntity>(jsonElement.GetRawText());
                            if (incomingChillEntity != null)
                                parsedValue = dbx.Find(targetType, incomingChillEntity.Guid);
                            else
                                parsedValue = null;
                            // Finalize
                            ef_prop.SetValue(Entity, parsedValue);
                        }
                        // CHILL-ENTITIES-COLLECTIONS
                        else if (typeof(IEnumerable<IChillEntity>).IsAssignableFrom(ef_prop.PropertyType))
                        {
                            parsedValue = null;
                            var incomingCollection = JsonSerializer.Deserialize<IEnumerable<ChillDtoEntity>>(jsonElement.GetRawText());

                            if (incomingCollection != null)
                            {
                                dbx.Entry(Entity).Collection(propertyName).Load();

                                Type collectionElementType = ef_prop.PropertyType
                                    .GetInterfaces()
                                    .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .First()
                                    .GetGenericArguments()[0];

                                // Create List<TTarget>
                                var listType = typeof(List<>).MakeGenericType(collectionElementType);
                                var targetList = (IList?)Activator.CreateInstance(listType);
                                foreach (var item in incomingCollection)
                                {
                                    targetList!.Add(dbx.Find(collectionElementType, item.Guid));
                                }

                                parsedValue = targetList;
                            }
                            // Finalize
                            ef_prop.SetValue(Entity, parsedValue);
                        }
                        else
                        {
                            parsedValue = JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType);
                            // Finalize
                            ef_prop.SetValue(Entity, parsedValue);
                        }
                    }
                    else
                    {
                        // OTHER PROPERTY TYPES
                        ef_prop.SetValue(Entity, parsedValue);
                    }
                }
                catch (Exception ex) 
                {
                    throw new ChillException($"Error setting value to field {propertyName} on chillable entity", ex); // {entity.GetFullTypeId()}
                }
            }
        }
        #endregion
    }
}
