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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChillSharp.Tests.EF.Query
{
    [ChillEntity(
        UniquePropertyKeyString: "3FA098F5-4929-4DD7-B951-DA641A3DFEED",
        PrimaryLanguageLabel: "Blog query",
        SecondaryLanguageLabel: "Ricerca Blog")]
    public class BlogQuery : ChillQuery
    {

        [ChillProperty(
            UniquePropertyKeyString: "AF868D2D-360A-485C-904D-DAFD7A830A8A",
            PrimaryLanguageLabel: "Blog title",
            SecondaryLanguageLabel: "Titolo del blog")]
        public string Title { get; set; } = string.Empty;

        public override void OnAutocomplete(IChillContext Context)
        {
            if (!string.IsNullOrWhiteSpace(Title) && !Guid.HasValue)
            {
                Title = Title.Trim();
                FullTextSearch = $"{Title} autocomplete";
            }
        }

        public override IEnumerable<ChillValidationError> OnValidation(IChillContext Context)
        {
            if (!string.Equals(Title?.Trim(), "invalid", StringComparison.OrdinalIgnoreCase))
                return [];

            return
            [
                new ChillValidationError
                {
                    FieldName = nameof(Title),
                    Message = "Blog query title is invalid."
                }
            ];
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override IQueryable<IChillEntity> OnQuery(IChillContext Context)
        {
            var ctx = (DummyContext)Context;
            var q = ctx.Blog.AsQueryable();
            if (Guid.HasValue)
                q = q.Where(x => x.Guid == Guid.Value);
            return q;
        }
    }
}
