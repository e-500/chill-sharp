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

namespace ChillSharp.Examples.BloggingApiService.Model
{
    public partial class Post : ChillEntity
    {
        /// <summary>
        /// <para>Init CreatedAt field</para>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="Context"></param>
        public override void OnCreate(IChillContext Context)
        {
            CreatedAt = DateTime.Now;
            base.OnCreate(Context);
        }

        /// <summary>
        /// <para>Use Title as label or default value</para>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="Context"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override string GetLabel(IChillContext Context)
        {
            return Title ?? base.GetLabel(Context);
        }

        /// <summary>
        /// <para>Use title and first 255 chars of content for full-text search representation</para>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        public override string GetFullTextContent(IChillContext Context)
        {
            return (Title ?? "") + " " + (Content.Length < 255 ? Content : Content.Substring(0, 255));
        }
    }
}
