#nullable enable
using System;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Marks a controller or action method as excluded from MCP tool discovery.
/// Applied at controller-level: all actions in that controller are excluded.
/// Applied at action-level: only that action is excluded.
/// Use this to hide internal or unsafe endpoints from tool exposure.
/// Example: [ExcludeFromTools] or [ExcludeFromTools("This endpoint requires direct authentication")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ExcludeFromToolsAttribute : Attribute
{
    /// <summary>
    /// Optional reason for exclusion (for documentation/code review purposes).
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Marks a controller or action as excluded from MCP tool discovery.
    /// </summary>
    public ExcludeFromToolsAttribute(string? reason = null)
    {
        this.Reason = reason;
    }
}
