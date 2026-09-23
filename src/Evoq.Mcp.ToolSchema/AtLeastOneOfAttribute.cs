#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// At least one of the listed properties must be set.
/// Emitted in JSON Schema as anyOf.
/// Apply at class level. Property names are C# names; schema builder resolves to JSON (camelCase) names.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AtLeastOneOfAttribute : Attribute
{
    /// <summary>
    /// C# property names; at least one must be present/set.
    /// </summary>
    public IReadOnlyList<string> PropertyNames { get; }

    /// <summary>
    /// At least one of the given properties must be provided.
    /// </summary>
    public AtLeastOneOfAttribute(params string[] propertyNames)
    {
        if (propertyNames == null || propertyNames.Length < 2)
        {
            throw new ArgumentException("At least two property names must be specified for AtLeastOneOf", nameof(propertyNames));
        }

        this.PropertyNames = propertyNames.ToList().AsReadOnly();
    }
}
