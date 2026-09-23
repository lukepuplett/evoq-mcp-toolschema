#nullable enable
using System;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Overrides the convention-derived MCP tool name for an action method.
/// When absent, the tool name is derived from the controller and action names via ToolNameConverter.
/// When present, the specified name is used verbatim (must be a valid snake_case MCP tool name).
/// Example: [ToolName("account_submit_feedback")]
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ToolNameAttribute : Attribute
{
    /// <summary>
    /// The exact MCP tool name to use for this action.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Overrides the convention-derived tool name with the specified value.
    /// </summary>
    public ToolNameAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tool name cannot be null or empty.", nameof(name));
        }

        this.Name = name;
    }
}
