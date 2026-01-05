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

using ChillSharp.EF;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Examples.BloggingApiService.Model
{
    public partial class Blog : ChillEntity, IChillEntity
    {
        /// <summary>
        /// Remove all blog posts before deltete it
        /// </summary>
        /// <param name="Context"></param>
        public override void OnDelete(IChillContext Context)
        {
            var db = (BloggingContext)Context;
            // Remove all posts having this as FK.
            db.Posts.Where(x => x.Blog == this).ExecuteDelete();
            base.OnDelete(Context);
        }

        /// <summary>
        /// Using blog Name as entity label<br/>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="Context"></param>
        /// <returns><inheritdoc/></returns>
        public override string GetLabel(IChillContext Context)
        {
            return Name ?? base.GetLabel(Context);
        }

        public override void OnInflate(IChillContext Context, string PropertyName)
        {
            var ctx = (BloggingContext)Context;
            if (PropertyName == "PostTitles")
            {
                // Load from db only post titles
                PostTitles = ctx.Posts.Where(x => x.Blog == this).Select(x => x.Title);
            }
            base.OnInflate(Context, PropertyName);
        }
    }
}
