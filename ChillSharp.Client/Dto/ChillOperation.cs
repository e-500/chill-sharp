/*
 * Author: Andrea Piovesan
 * Year: 2025
 * License: GNU Affero General Public License (AGPL) version 3
 *
 * Disclaimer:
 * You are free to use, modify, and distribute it under the terms of the AGPL v3 license.
 * This code comes with no warranty; use it at your own risk.
 * 
 * For further information, please refer to README and LICENSE files.
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
