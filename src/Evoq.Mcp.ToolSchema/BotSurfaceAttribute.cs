#nullable enable
using System;
using System.Collections.Generic;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Marks a controller or action method as belonging to one or more MCP surface areas.
/// Optionally restricts the surface to specific communication channels.
/// Applied at controller-level: all actions in that controller belong to the surface(s).
/// Applied at action-level: only that action belongs to the surface(s); additive with controller-level.
///
/// Examples:
/// [BotSurface("journal")] — available on all channels
/// [BotSurface("journal", channel: "MCP")] — available only on MCP channel
/// [BotSurface("journal", channel: "WhatsApp")] — available only on WhatsApp channel
/// Use repeated attributes for multiple channels:
/// [BotSurface("journal", channel: "MCP")]
/// [BotSurface("journal", channel: "WhatsApp")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class BotSurfaceAttribute : Attribute
{
    /// <summary>
    /// One or more surface names this controller/action belongs to.
    /// </summary>
    public IReadOnlyCollection<string> Surfaces { get; }

    /// <summary>
    /// Optional channel name restricting this surface to a specific communication channel.
    /// If null, the surface is available on all channels.
    /// </summary>
    public string? Channel { get; }

    /// <summary>
    /// Marks a controller or action as belonging to one or more MCP surface areas.
    /// Available on all channels.
    /// </summary>
    public BotSurfaceAttribute(params string[] surfaces)
    {
        if (surfaces == null || surfaces.Length == 0)
        {
            throw new ArgumentException("At least one surface name must be specified", nameof(surfaces));
        }

        this.Surfaces = Array.AsReadOnly(surfaces);
        this.Channel = null;
    }

    /// <summary>
    /// Marks a controller or action as belonging to a surface, restricted to a specific channel.
    /// </summary>
    public BotSurfaceAttribute(string surfaceName, string channel)
    {
        if (string.IsNullOrWhiteSpace(surfaceName))
        {
            throw new ArgumentException("Surface name cannot be null or empty", nameof(surfaceName));
        }

        if (string.IsNullOrWhiteSpace(channel))
        {
            throw new ArgumentException("Channel name cannot be null or empty", nameof(channel));
        }

        this.Surfaces = Array.AsReadOnly(new[] { surfaceName });
        this.Channel = channel;
    }
}
