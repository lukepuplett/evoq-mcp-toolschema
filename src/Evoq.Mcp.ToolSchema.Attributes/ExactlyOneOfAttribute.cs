#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Exactly one of the listed properties must be set.
/// Emitted in JSON Schema as oneOf (exactly one of the subschemas valid).
/// Apply at class level. Property names are C# names; schema builder resolves to JSON (camelCase) names.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ExactlyOneOfAttribute : Attribute
{
    /// <summary>
    /// C# property names; exactly one must be present/set.
    /// </summary>
    public IReadOnlyList<string> PropertyNames { get; }

    /// <summary>
    /// Exactly one of the given properties must be provided.
    /// </summary>
    public ExactlyOneOfAttribute(params string[] propertyNames)
    {
        if (propertyNames == null || propertyNames.Length < 2)
        {
            throw new ArgumentException("At least two property names must be specified for ExactlyOneOf", nameof(propertyNames));
        }

        this.PropertyNames = propertyNames.ToList().AsReadOnly();
    }
}
