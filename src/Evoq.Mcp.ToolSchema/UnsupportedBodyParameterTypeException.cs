#nullable enable
using System;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Thrown when a [FromBody] action parameter is a primitive, enum, Guid, DateTime, or bare collection
/// type rather than a request DTO. Schema generation reflects the body type's public properties to
/// build the tool's inputSchema; a primitive exposes irrelevant CLR members (e.g. string.Length,
/// string.Chars) instead of a scalar value, and a bare collection exposes List&lt;T&gt;/array members
/// instead of an array schema. Proper model binding — a class or record DTO — is required for body
/// parameters so the generated schema reflects the actual request shape.
/// </summary>
public sealed class UnsupportedBodyParameterTypeException : Exception
{
    /// <summary>
    /// The offending body parameter's CLR type.
    /// </summary>
    public Type ParameterType { get; }

    public UnsupportedBodyParameterTypeException(Type parameterType)
        : base(
            $"Unable to build MCP input schema for body parameter of type '{parameterType.Name}'. " +
            "[FromBody] parameters must be a request DTO (a class or record with public properties), " +
            "not a primitive, enum, Guid, DateTime, or bare collection type. Wrap the value in a request " +
            $"type, e.g. 'public sealed record {parameterType.Name}Request({parameterType.Name} Value);', and bind that instead.")
    {
        this.ParameterType = parameterType;
    }
}
