// PSEUDOCODE / PLAN (detailed):
// - Create a cache class named `ChillApiSchemaCache` to store schemas for fast retrieval.
//   - Use a thread-safe dictionary keyed by "<ChillType>|<ChillViewCode>" for O(1) lookup.
//   - Expose a read-only collection of schemas via `Schemas` property.
//   - Provide methods:
//     - `TryGet(string chillType, string chillViewCode, out ChillDtoSchema schema)` to attempt retrieval.
//     - `SetSchema(ChillDtoSchema schema)` to add/update the cache entry.
//     - `Invalidate(string chillType, string chillViewCode)` to remove a single entry.
//     - `InvalidateAll()` to clear the cache.
// - Add a static holder `ChillExtention` with a singleton `ChillApiSchemaCache` property,
//   similar to how `ChillApiOptions` is expected to be exposed.
// - Ensure types are in the `ChillSharp` namespace and reference `ChillSharp.Dto` for `ChillDtoSchema`.
// - Keep the cache implementation lightweight and thread-safe using `ConcurrentDictionary`.
// - Consumers (ChillDtoEngine) will use `ChillExtention.ChillApiSchemaCache` to look up or update cache.
//
// After this file is added, update `ChillDtoEngine`:
// - In `GetSchema`, try returning cached schema first.
// - If not cached, load file (or create default) then store it in the cache.
// - In `SetSchema`, after writing the file, set/update the schema in the cache so subsequent `GetSchema` uses it.
// - Ensure `null`/empty values fall back to `"default"` for cache keys.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ChillSharp.Dto;

namespace ChillSharp.Dto
{
    /// <summary>
    /// Thread-safe in-memory cache for Chill API schemas.
    /// </summary>
    public sealed class ChillDtoSchemaCache : IChillDtoSchemaCache
    {
        private readonly ConcurrentDictionary<string, ChillDtoSchema> _cache = new();

        /// <summary>
        /// Returns a snapshot list of cached schemas.
        /// </summary>
        public IReadOnlyCollection<ChillDtoSchema> Schemas => _cache.Values.ToList().AsReadOnly();

        private static string MakeKey(string chillType, string chillViewCode)
        {
            var t = string.IsNullOrWhiteSpace(chillType) ? "default" : chillType!;
            var v = string.IsNullOrWhiteSpace(chillViewCode) ? "default" : chillViewCode!;
            return $"{t}|{v}";
        }

        /// <summary>
        /// Attempts to retrieve a schema from the cache.
        /// </summary>
        public bool TryGet(string chillType, string chillViewCode, out ChillDtoSchema? schema)
        {
            return _cache.TryGetValue(MakeKey(chillType, chillViewCode), out schema);
        }

        /// <summary>
        /// Adds or updates a schema in the cache.
        /// Returns the added/updated schema.
        /// </summary>
        public ChillDtoSchema SetSchema(ChillDtoSchema schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            var key = MakeKey(schema.ChillType, schema.ChillViewCode);
            _cache.AddOrUpdate(key, schema, (_, __) => schema);
            return schema;
        }

        /// <summary>
        /// Removes a single schema from the cache.
        /// </summary>
        public void Invalidate(string chillType, string chillViewCode)
        {
            _cache.TryRemove(MakeKey(chillType, chillViewCode), out _);
        }

        /// <summary>
        /// Clears the entire cache.
        /// </summary>
        public void InvalidateAll()
        {
            _cache.Clear();
        }
    }
}