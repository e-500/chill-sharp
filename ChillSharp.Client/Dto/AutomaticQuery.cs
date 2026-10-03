/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using System.Text.Json.Serialization;

namespace ChillSharp.Client.Dto;

/// <summary>A client-side automatic query definition sent to the Chill query endpoint.</summary>
public sealed class AutomaticQuery
{
    public AutomaticQueryGroup Filter { get; set; } = new();
}

public sealed class AutomaticQueryGroup
{
    public AutomaticQueryLogicalOperator LogicalOperator { get; set; } = AutomaticQueryLogicalOperator.And;
    public IList<AutomaticQueryFilter> Filters { get; set; } = new List<AutomaticQueryFilter>();
    public IList<AutomaticQueryGroup> Groups { get; set; } = new List<AutomaticQueryGroup>();
}

public sealed class AutomaticQueryFilter
{
    public string PropertyName { get; set; } = string.Empty;
    public AutomaticQueryOperator Operator { get; set; } = AutomaticQueryOperator.Equal;
    public object? Value { get; set; }
    public object? SecondValue { get; set; }
    public AutomaticQueryGroup? ItemFilter { get; set; }
    public bool IgnoreCase { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AutomaticQueryLogicalOperator
{
    And,
    Or
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AutomaticQueryOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Between,
    Contains,
    StartsWith,
    EndsWith,
    In,
    IsNull,
    IsNotNull,
    IsEmpty,
    IsNotEmpty,
    Any,
    All
}
