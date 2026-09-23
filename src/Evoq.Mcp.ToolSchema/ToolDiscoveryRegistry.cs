#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Discovers qualifying tool actions across controller types and builds a registry.
///
/// Responsibility:
/// - For each controller type: find qualifying actions ([Http*] + [Description])
/// - Derive tool names (snake_case) for each action
/// - Store mapping: toolName → ToolRegistration (with controller type, method, parameter types)
/// - Provides a single source of truth for tool discovery, used by both schema generation and dispatch
///
/// Usage:
///   var registry = new ToolDiscoveryRegistry(typeof(JournalApiController), typeof(WorkflowsApiController));
///   foreach (var (toolName, toolReg) in registry.Tools)
///   {
///       Console.WriteLine($"{toolName} → {toolReg.ControllerType.Name}.{toolReg.ActionMethod.Name}");
///   }
/// </summary>
public sealed class ToolDiscoveryRegistry
{
    private readonly IReadOnlyDictionary<string, ToolRegistration> tools;

    /// <summary>
    /// Constructs a registry by discovering tools in the given controller types.
    ///
    /// For each controller type:
    /// 1. Uses QualifyingActionDiscoverer to find qualifying actions
    /// 2. Uses ToolNameConverter to derive tool names
    /// 3. Extracts parameter type information
    /// 4. Stores in the registry
    /// </summary>
    public ToolDiscoveryRegistry(params Type[] controllerTypes)
    {
        var discoverer = new QualifyingActionDiscoverer();
        var registryBuilder = new Dictionary<string, ToolRegistration>();

        foreach (var controllerType in controllerTypes ?? Array.Empty<Type>())
        {
            var qualifyingActions = discoverer.Discover(controllerType);

            foreach (var (method, _, _) in qualifyingActions)
            {
                var toolNameAttr = method.GetCustomAttribute<ToolNameAttribute>();
                var toolName = toolNameAttr?.Name ?? ToolNameConverter.ToToolName(controllerType.Name, method.Name);
                var parameterTypes = ExtractParameterTypes(method);

                if (registryBuilder.ContainsKey(toolName))
                {
                    throw new InvalidOperationException(
                        $"Duplicate tool name '{toolName}' discovered. " +
                        $"Tool names must be unique across all controller types.");
                }

                registryBuilder[toolName] = new ToolRegistration(
                    controllerType,
                    method,
                    parameterTypes);
            }
        }

        this.tools = registryBuilder;
    }

    /// <summary>
    /// Read-only registry: tool name → metadata needed to generate schema or invoke the tool.
    /// </summary>
    public IReadOnlyDictionary<string, ToolRegistration> Tools => this.tools;

    /// <summary>
    /// Extracts parameter type information from a controller action method.
    ///
    /// Returns a list of (Type, bool IsBody) for each parameter:
    /// - Type: the parameter's type
    /// - IsBody: true if marked with [FromBody], false for [FromRoute]/[FromQuery]
    ///
    /// Implementation: Inspects method parameters and their [FromBody], [FromRoute], [FromQuery] attributes.
    /// </summary>
    private static IReadOnlyList<(Type Type, bool IsBody)> ExtractParameterTypes(MethodInfo method)
    {
        var paramList = new List<(Type Type, bool IsBody)>();

        foreach (var param in method.GetParameters())
        {
            // Check for [FromBody] attribute by name (since it's from Microsoft.AspNetCore.Mvc)
            var isBody = param.CustomAttributes.Any(a => a.AttributeType.Name == "FromBodyAttribute");

            paramList.Add((param.ParameterType, isBody));
        }

        return paramList.AsReadOnly();
    }
}
