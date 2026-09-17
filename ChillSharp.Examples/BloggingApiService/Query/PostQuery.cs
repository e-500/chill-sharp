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
using ChillSharp.Examples.BloggingApiService.Model;

namespace ChillSharp.Examples.BloggingApiService.Query
{
    public class PostQuery : ChillQuery
    {
        /// <summary>
        /// Filter blogs by Content (contains)
        /// </summary>
        [ChillProperty]
        public string? Content { get; set; }

        /// <summary>
        /// Filter blogs by Name (contains)
        /// </summary>
        [ChillProperty]
        public Blog? Blog { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override IQueryable<IChillEntity> OnQuery(IChillContext context)
        {
            var db = ((BloggingContext)context);
            var q = db.Posts.AsQueryable();

            if (!string.IsNullOrEmpty(Content))
                q = q.Where(x => x.Content.Contains(Content));

            if (Blog != null)
                q = q.Where(x => x.Blog == Blog);

            return q;
        }
    }
}
