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

using ChillSharp.Auth.Model;

namespace ChillSharp.Auth.Contracts;

/// <summary>
/// Describes the resolved outcome of a permission evaluation request.
/// </summary>
public class PermissionEvaluationResult
{
    /// <summary>
    /// Gets or sets whether the requested action is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Gets or sets the effect of the matched rule, if any.
    /// </summary>
    public PermissionEffect? MatchedEffect { get; set; }

    /// <summary>
    /// Gets or sets a human-readable explanation of the resolution result.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the rule that produced the result.
    /// </summary>
    public Guid? RuleGuid { get; set; }

    /// <summary>
    /// Gets or sets whether the matched rule came from a user or role assignment.
    /// </summary>
    public string? RuleSource { get; set; }
}

/// <summary>
/// Contains a permission evaluation result for a single property.
/// </summary>
public class PropertyPermissionResult
{
    /// <summary>
    /// Gets or sets the name of the evaluated property.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the evaluation result for the property.
    /// </summary>
    public PermissionEvaluationResult Result { get; set; } = new();
}

/// <summary>
/// Groups permission evaluation results for multiple properties.
/// </summary>
public class PropertyPermissionSetResult
{
    /// <summary>
    /// Gets or sets the evaluated property results.
    /// </summary>
    public IReadOnlyList<PropertyPermissionResult> Properties { get; set; } = Array.Empty<PropertyPermissionResult>();
}
