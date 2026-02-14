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
    /// Represents a lightweight, serializable Data Transfer Object (DTO) for defining and executing
    /// dynamic queries or commands using the ChillSharp engine.
    ///
    /// <para>
    /// The <see cref="ChillDtoQuery"/> is designed to be **web-friendly**, omitting EF Core
    /// navigation properties and collection structures. It allows clients to:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Provide query parameters or command arguments via <see cref="Fields"/>.</description></item>
    ///   <item><description>Execute the query or command on the server through the Chill API engine.</description></item>
    ///   <item><description>Receive the results back via the <see cref="Results"/> collection.</description></item>
    /// </list>
    ///
    /// <para>
    /// This class acts as a generic carrier between the client and the ChillSharp engine,
    /// enabling flexible, type-safe data operations without exposing internal EF Core entities.
    /// </para>
    ///
    /// <para>Licensing:
    /// This code is part of the ChillSharp library, released under the GNU GENERAL PUBLIC LICENSE v3 (GPLv3).<br/>
    /// Any modification or redistribution must comply with the GPLv3 license terms.<br/>
    /// For commercial or LGPL licensing options, please contact the author.<br/>
    /// © 2025 Andrea Piovesan
    /// </para>
    /// </summary>
    public class ChillDtoQuery : IDtoChillable
    {
        /// <summary>
        /// Optional string identifying the entity type or category.
        /// Useful for polymorphic handling of different entity types in a generic web model.
        /// </summary>
        public string ChillType { get; set; } = string.Empty;

		/// <summary>
		/// A dictionary mapping field names (property keys) to their corresponding values.
		/// Each value is wrapped in a ChillFieldValue, which includes metadata about the field type.
		/// This allows for flexible, dynamic serialization of entity fields.
		/// </summary>
		public Dictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();

        /// <summary>
        /// List of properties of the entity to be obtained with this query
        /// </summary>
        public List<ChillDtoProperty>? ResultProperties { get; set; } = null;

		/// <summary>
		/// A list of entities returned as the result of query execution.
		/// This collection remains empty until the query is executed by the ChillSharp engine.
		/// </summary>
		public List<ChillDtoEntity> Results { get; set; } = new List<ChillDtoEntity>();

        #region HELPERS

        /// <summary>
        /// Verifies that the query type has a valid ChillType prefix and returns its short identifier form.
        /// </summary>
        /// <param name="Context">The current Chill context used for validation.</param>
        /// <param name="Query">The query instance to test.</param>
        /// <returns>The short form of the Chill type identifier.</returns>
        /// <exception cref="ChillException">Thrown if the query type name is invalid or improperly prefixed.</exception>
        private string _TestEntityAndGetChillType(IChillContext Context, IChillQuery<IChillEntity> Query)
        {
            var chillType = Query.GetType().FullName;
            var chillTypePrefix = Context.GetChillTypePrefix();
            if (string.IsNullOrEmpty(chillType))
                throw new ChillException($"Entity type full name ({chillType}) is invalid");
            if (!chillType.StartsWith(chillTypePrefix))
                throw new ChillException($"Entity type full name ({chillType}) doesn't start with {chillTypePrefix}");

            return chillType.Substring(chillTypePrefix.Length + 1);
        }

		/// <summary>
		/// Initializes this DTO query from an existing Chill query object.
		/// Extracts annotated properties and their values, storing them in the <see cref="Fields"/> collection.
		/// </summary>
		/// <param name="Context">The Chill context providing type information.</param>
		/// <param name="Query">The query object to serialize into this DTO.</param>
		public void FromQuery(IChillContext Context, IChillQuery<IChillEntity> Query)
        {
			// Test and get main fields from chill entity
			ChillType = _TestEntityAndGetChillType(Context, Query);

			var ef_props = Query.GetType().GetProperties().Where(prop => 
                prop.IsDefined(typeof(ChillPropertyAttribute), false));

            var dbx = (DbContext)Context;

            Properties = ef_props.ToDictionary(
				ef_prop => ef_prop.Name,
				ef_prop => {
					var attr = (ChillPropertyAttribute)ef_prop.GetCustomAttributes(typeof(ChillPropertyAttribute), false).First();
                    var propertyName = ef_prop.Name; // TODO consent to override internal name using a custom property on ChillPropertyAttribute

                    // CHILL-ENTITY
                    if (typeof(IChillEntity).IsAssignableFrom(ef_prop.PropertyType))
                    {
                        // if is a reference load it and convert to ChillDtoEntity
                        if (dbx.Entry(Query).Reference(propertyName).Exist(true))
                        {
                            var ef_obj = (IChillEntity?)ef_prop.GetValue(Query);
                            // NULL
                            if (ef_obj == null)
                                return null;

                            //ChillDtoProperty? reqProp = null;
                            //if (RequiredProperties != null)
                            //    reqProp = RequiredProperties.Where(x => x.PropertyName == propertyName).FirstOrDefault();
                            return new ChillDtoEntity(Context, ef_obj, null); // (reqProp?.SubProperties ?? new List<ChillDtoProperty>()));
                        }
                        return null;
                    }
                    // CHILL-ENTITIES-COLLECTIONS
                    else if (typeof(IEnumerable<IChillEntity>).IsAssignableFrom(ef_prop.PropertyType))
                    {
                        // Try to load collection 
                        dbx.Entry(Query).Collection(propertyName)?.Load();

                        var entity = (IEnumerable<IChillEntity>?)ef_prop.GetValue(Query);
                        if (entity != null)
                        {
                            var ef_obj_coll = (IEnumerable<IChillEntity>?)ef_prop.GetValue(Query);
                            // NULL
                            if (ef_obj_coll == null)
                                return null;

                            return ef_obj_coll.Select(ef_obj => {
                            //ChillDtoProperty? reqProp = null;
                            //if (RequiredProperties != null)
                            //    reqProp = RequiredProperties.Where(x => x.PropertyName == propertyName).FirstOrDefault();
                            return new ChillDtoEntity(Context, ef_obj, null); // (reqProp?.SubProperties ?? new List<ChillDtoProperty>()));
                            });
                        }
                        else
                            return null;
                    }
                    // OTHER PROPERTY TYPES
                    else
                    {
                        return (object?)ef_prop.GetValue(Query);
                    }
                });
		}

        /// <summary>
        /// Applies values from this DTO query to a Chill query instance.
        /// The receiving object must have matching field names annotated with <see cref="ChillPropertyAttribute"/>.
        /// </summary>
        /// <param name="Context">The current Chill context for validation and mapping.</param>
        /// <param name="Query">The target query instance to populate with DTO data.</param>
        /// <exception cref="ChillException">Thrown if type validation or property assignment fails.</exception>
        public void ToQuery(IChillContext Context, IChillQuery<IChillEntity> Query)
        {
            // Test only if Entity is a valid chill entity
            string QueryChillType = _TestEntityAndGetChillType(Context, Query);
            if (ChillType != QueryChillType)
                throw new ChillException($"Entity ChillType ({QueryChillType}) differs from Dto ChillType ({ChillType})");

            var dbx = (DbContext)Context;
            if (dbx == null)
                throw new ChillException("Can't cast to IChillContext to DbContext");

            var ef_props = Query.GetType().GetProperties()
                .Where(prop => prop.IsDefined(typeof(ChillPropertyAttribute), false))
                .Where(x => Properties.Keys.Contains(x.Name));
            foreach (var ef_prop in ef_props)
            {
                var attr = (ChillPropertyAttribute)ef_prop.GetCustomAttributes(typeof(ChillPropertyAttribute), false).First();
                var propertyName = ef_prop.Name; // TODO consent to override internal name using a custom property on ChillPropertyAttribute
				var value = Properties[ef_prop.Name];
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
                            ef_prop.SetValue(Query, parsedValue);
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
                            ef_prop.SetValue(Query, parsedValue);
                        }
                        // CHILL-ENTITIES-COLLECTIONS
                        else if (false && typeof(IEnumerable<IChillEntity>).IsAssignableFrom(ef_prop.PropertyType))
                        {
                            parsedValue = null;
                            var incomingCollection = JsonSerializer.Deserialize<IEnumerable<ChillDtoEntity>>(jsonElement.GetRawText());

                            if (incomingCollection != null)
                            {
                                // In a query object nothing to load
                                //dbx.Entry(Entity).Collection(propertyName).Load();

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
                            ef_prop.SetValue(Query, parsedValue);
                        }
                        else
                        {
                            parsedValue = JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType);
                            // Finalize
                            ef_prop.SetValue(Query, parsedValue);
                        }
                    }
                    else
                    {
                        // OTHER PROPERTY TYPES
                        ef_prop.SetValue(Query, parsedValue);
                    }
                }
                catch (Exception ex)
                {
                    throw new ChillException($"Error setting value to field {propertyName} on chillable query", ex); 
                }
            }
        }
        #endregion
    }
}
