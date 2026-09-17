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

﻿using ChillSharp.Annotations;
using ChillSharp.EF;
using ChillSharp.Tests.EF.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChillSharp.Tests.EF.Query
{
    [ChillEntity(
        UniquePropertyKeyString: "0FE6425D-6C0F-402B-A63E-2A671CCA85E1",
        PrimaryLanguageLabel: "Post query",
        SecondaryLanguageLabel: "Ricerca Post")]
    public class PostQuery : ChillQuery
    {
        [ChillProperty(
            UniquePropertyKeyString: "34FA82DC-5DAF-4BA8-978F-D94CE2564240",
            PrimaryLanguageLabel: "Title",
            SecondaryLanguageLabel: "Titolo")]
        public string Title { get; set; } = string.Empty;

        [ChillProperty(
            UniquePropertyKeyString: "34B64688-F1BB-444D-94D8-2E9670346F29",
            PrimaryLanguageLabel: "Blog",
            SecondaryLanguageLabel: "Blog")]
        public Blog? Blog { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override IQueryable<IChillEntity> OnQuery(IChillContext Context)
        {
            var ctx = (DummyContext)Context;
            var q = ctx.Post.AsQueryable();
            if (Guid.HasValue)
                q = q.Where(x => x.Guid == Guid.Value);
            if (Blog != null)
                q = q.Where(x => x.Blog == Blog);
            return q;
        }
    }
}
