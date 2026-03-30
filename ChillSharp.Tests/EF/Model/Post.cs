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
        UniquePropertyKeyString: "13673DEB-15DA-439B-8AEA-7C5FB3BA8C79",
        PrimaryLanguageLabel: "Post",
        SecondaryLanguageLabel: "Post",
        MCPDescription = "Post resource exposed to MCP clients.")]
    public class Post : ChillEntity
    {
        [Key]
        public override Guid Guid { get; set; }

        [ChillProperty(
            UniquePropertyKeyString: "0636356D-C6C1-4319-9E1F-121CF87CE617",
            PrimaryLanguageLabel: "Blog",
            SecondaryLanguageLabel: "Blog",
            MCPDescription = "Owning blog for the post.")]
        public Blog? Blog { get; set; } = null;

        [ChillProperty(
            UniquePropertyKeyString: "81093FE4-6DA2-4AD3-AAA0-FB24B36031C2",
            PrimaryLanguageLabel: "Post title",
            SecondaryLanguageLabel: "Titolo del post",
            MCPDescription = "Post title visible to MCP clients.")]
        public string Title { get; set; } = string.Empty;

        [ChillProperty(
            UniquePropertyKeyString: "27519E40-CDB7-48AE-AD78-3C530BD4F3A7",
            PrimaryLanguageLabel: "Post title",
            SecondaryLanguageLabel: "Titolo del post",
            MCPDescription = "Author of the post.")]
        public string Author { get; set; } = string.Empty;
    }
}
