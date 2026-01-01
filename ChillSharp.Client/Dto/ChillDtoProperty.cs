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
    public class ChillDtoProperty
    {
        public ChillDtoProperty() { }
        public ChillDtoProperty(string PropertyName) { this.PropertyName = PropertyName; }
        public string PropertyName { get; set; } = string.Empty;
        public List<ChillDtoProperty>? SubProperties { get; set; } = null;

        public static List<ChillDtoProperty> FromStrings(string[] Array)
        {
            return Array.Select(x => new ChillDtoProperty(x)).ToList();
        }

        public static List<ChillDtoProperty> FromStrings(List<string> List)
        {
            return List.Select(x => new ChillDtoProperty(x)).ToList();
        }
    }
}
