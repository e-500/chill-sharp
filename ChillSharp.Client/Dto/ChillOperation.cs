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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChillSharp.Client.Dto
{
    /// <summary>
    /// Represents a single operation within a chunked sequence sent to the ChillSharp API.
    /// </summary>
    public class ChillOperation
    {
        /// <summary>
        /// The order in which this operation should be executed.
        /// </summary>
        public int Index { get; set; } = 0;

        /// <summary>
        /// The verb describing what action should be performed (query, create, update, etc.).
        /// </summary>
        public string? Verb { get; set; }

        /// <summary>
        /// The query object to execute, if applicable.
        /// </summary>
        public ChillDtoQuery? Query { get; set; }

        /// <summary>
        /// The entity object to act upon, if applicable.
        /// </summary>
        public ChillDtoEntity? Entity { get; set; }
    }

    /// <summary>
    /// Defines all verbs supported by a ChillOperation.
    /// </summary>
    public static class ChillOperationVerb
    {
        public const string TRANSACTION  = "transaction";
        public const string QUERY  = "query";
        //public const string FIND   = "find";
        public const string CREATE = "create";
        public const string UPDATE = "update";
        public const string DELETE = "delete";
        public const string COMMIT = "commit";
    }
}
