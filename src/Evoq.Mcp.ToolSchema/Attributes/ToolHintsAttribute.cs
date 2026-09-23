#nullable enable
using System;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Declares MCP tool safety hint annotations for an action method.
/// Emitted in the tools/list response as the "annotations" object per the MCP 2025-03-26 spec.
///
/// Use this only to override the convention-based defaults applied by McpToolDefinitionBuilder:
///   HttpGet  → readOnly=true,  destructive=false, openWorld=false
///   HttpDelete → readOnly=false, destructive=true,  openWorld=false
///   All other HTTP methods → all false
///
/// Examples where an explicit override is needed:
///   HttpPost that permanently deletes:  [ToolHints(destructive: true)]
///   HttpGet that calls an external API: [ToolHints(openWorld: true)]
///   HttpPost that sends a notification: [ToolHints(openWorld: true)]
///   HttpPost that deletes AND notifies: [ToolHints(destructive: true, openWorld: true)]
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ToolHintsAttribute : Attribute
{
    public bool ReadOnly { get; }
    public bool Destructive { get; }
    public bool OpenWorld { get; }
    public bool Idempotent { get; }

    public ToolHintsAttribute(
        bool readOnly = false,
        bool destructive = false,
        bool openWorld = false,
        bool idempotent = false)
    {
        this.ReadOnly = readOnly;
        this.Destructive = destructive;
        this.OpenWorld = openWorld;
        this.Idempotent = idempotent;
    }
}
