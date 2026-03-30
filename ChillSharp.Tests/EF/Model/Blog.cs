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

namespace ChillSharp.Tests.EF.Model
{
    [ChillEntity(
        UniquePropertyKeyString: "C0D5C2FB-418C-4E5E-9462-CF2284C02403", 
        PrimaryLanguageLabel: "Blog", 
        SecondaryLanguageLabel: "Blog")]
    public class Blog : ChillEntity
    {
        [Key]
        public override Guid Guid { get; set; }

        [ChillProperty(
            UniquePropertyKeyString: "AF85D5B7-576F-4F38-A7DC-2C4FC317AFC7",
            PrimaryLanguageLabel: "Blog title",
            SecondaryLanguageLabel: "Titolo del blog")]
        public string Title { get; set; } = string.Empty;

        [ChillProperty(
            UniquePropertyKeyString: "D3FB9BC9-B5FB-495F-AD50-64899E950D80",
            PrimaryLanguageLabel: "Blog url",
            SecondaryLanguageLabel: "Url del blog")]
        public string Url { get; set; } = string.Empty;


        [ChillProperty(
            UniquePropertyKeyString: "9501DEFE-7504-45E4-884B-D2BAB3BE9701",
            PrimaryLanguageLabel: "Blog posts",
            SecondaryLanguageLabel: "Post del blog")]
        public ICollection<Post>? Posts { get; set; } = null;

        public override void OnAutocomplete(IChillContext Context)
        {
            if (!string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Url))
            {
                Url = $"https://autocomplete.local/{Title.Trim().ToLowerInvariant().Replace(' ', '-')}";
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
                    Message = "Blog title is invalid."
                }
            ];
        }
    }
}
