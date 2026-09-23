#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Batch generator: produces MCP tool definitions for all qualifying actions in a controller or assembly.
/// In: Type controllerType (or Assembly or Type[]).
/// Out: IReadOnlyList of tool definitions suitable for JSON serialization.
/// Logic: discover qualifying actions, build tool definition for each, return as list.
/// </summary>
public sealed class McpToolDefinitionsBatchGenerator
{
    private readonly QualifyingActionDiscoverer actionDiscoverer = new();
    private readonly McpToolDefinitionBuilder toolBuilder = new();

    /// <summary>
    /// Generates MCP tool definitions for all qualifying actions in a single controller.
    /// WARNING: Uses obsolete Discover() method. For new code, use GenerateForSurface() instead.
    /// </summary>
    [Obsolete("Use GenerateForSurface(controllerType, surfaceName) instead for surface-aware discovery.")]
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateForController(Type controllerType)
    {
        var results = new List<IReadOnlyDictionary<string, object?>>();

        var qualifyingActions = actionDiscoverer.Discover(controllerType);
        foreach (var (method, _, _) in qualifyingActions)
        {
            var toolDefinition = toolBuilder.Build(method);
            results.Add(toolDefinition);
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Generates MCP tool definitions for all qualifying actions in a single controller that belong to the specified surface.
    /// Optionally filters by channel. Uses DiscoverBySurface for surface-scoped discovery per the MCP architecture.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateForSurface(Type controllerType, string surfaceName, string? channelName = null)
    {
        var results = new List<IReadOnlyDictionary<string, object?>>();

        var qualifyingActions = actionDiscoverer.DiscoverBySurface(controllerType, surfaceName, channelName);
        foreach (var (method, _, _) in qualifyingActions)
        {
            var toolDefinition = toolBuilder.Build(method);
            results.Add(toolDefinition);
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Generates MCP tool definitions for all qualifying actions across multiple controllers for a specific surface.
    /// Optionally filters by channel.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateForSurface(IEnumerable<Type> controllerTypes, string surfaceName, string? channelName = null)
    {
        var results = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var controllerType in controllerTypes)
        {
            var toolsForController = GenerateForSurface(controllerType, surfaceName, channelName);
            results.AddRange(toolsForController);
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Generates MCP tool definitions for all qualifying actions in multiple controllers.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateForControllers(IEnumerable<Type> controllerTypes)
    {
        var results = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var controllerType in controllerTypes)
        {
            var toolsForController = GenerateForController(controllerType);
            results.AddRange(toolsForController);
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Generates MCP tool definitions for all qualifying actions in an assembly.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> GenerateForAssembly(System.Reflection.Assembly assembly)
    {
        // Find all controller types in the assembly
        var controllerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"))
            .ToList();

        return GenerateForControllers(controllerTypes);
    }
}
