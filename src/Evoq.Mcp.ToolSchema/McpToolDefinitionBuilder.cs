#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Orchestrator: turns one action MethodInfo into one MCP tool definition (name, description, inputSchema).
/// In: MethodInfo for the action.
/// Out: a dictionary suitable for JSON serialization as one MCP tool (name, description, inputSchema).
/// Logic: get tool name (ToolNameConverter), description ([Description] on method), parameter types (params vs body),
/// then InputSchemaBuilder to get inputSchema; return combined object.
/// </summary>
public sealed class McpToolDefinitionBuilder
{
    private readonly InputSchemaBuilder inputSchemaBuilder = new();

    public IReadOnlyDictionary<string, object?> Build(MethodInfo actionMethod)
    {
        // Get controller type and names
        var controllerType = actionMethod.DeclaringType!;
        var controllerName = ExtractControllerName(controllerType);
        var actionName = actionMethod.Name;

        // Get tool name — honour [ToolName] override, fall back to convention
        var toolNameAttr = actionMethod.GetCustomAttribute<ToolNameAttribute>();
        var toolName = toolNameAttr?.Name ?? ToolNameConverter.ToToolName(controllerType.Name, actionName);

        // Get description from [Description] attribute
        var descriptionAttr = actionMethod.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        var description = descriptionAttr?.Description ?? "";

        // Get method parameters and classify them
        var parameterTypes = ExtractAndClassifyParameters(actionMethod);

        // Build input schema by merging all parameter types
        var inputSchema = inputSchemaBuilder.Build(parameterTypes);

        // Derive base hints from HTTP method convention; [ToolHints] ORs additional flags on top.
        // Convention provides the floor — [ToolHints] can only raise flags, never lower them.
        // e.g. [ToolHints(openWorld:true)] on a GET keeps readOnly=true and adds openWorld=true.
        var isGet = actionMethod.GetCustomAttributes().Any(a => a.GetType().Name == "HttpGetAttribute");
        var isDelete = actionMethod.GetCustomAttributes().Any(a => a.GetType().Name == "HttpDeleteAttribute");

        var hintsAttr = actionMethod.GetCustomAttribute<ToolHintsAttribute>();

        var annotations = new Dictionary<string, object?>
        {
            { "readOnlyHint",    isGet    || (hintsAttr?.ReadOnly    ?? false) },
            { "destructiveHint", isDelete || (hintsAttr?.Destructive ?? false) },
            { "openWorldHint",              hintsAttr?.OpenWorld   ?? false },
            { "idempotentHint",             hintsAttr?.Idempotent  ?? false },
        };

        // Construct the MCP tool definition
        var result = new Dictionary<string, object?>
        {
            { "name", toolName },
            { "description", description },
            { "inputSchema", inputSchema },
            { "annotations", annotations },
        };

        return result;
    }

    /// <summary>
    /// Extracts parameters from the method and classifies them as params or body.
    /// Filters out infrastructure parameters (CancellationToken, etc.).
    /// </summary>
    private static IEnumerable<(Type Type, bool IsBody, string? ParameterName)> ExtractAndClassifyParameters(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var infrastructureTypeNames = new[] { "CancellationToken", "HttpContext" };

        foreach (var param in parameters)
        {
            // Skip infrastructure parameters by type name
            if (infrastructureTypeNames.Contains(param.ParameterType.Name))
            {
                continue;
            }

            // Skip parameters that are injected (not model-bound)
            // Check for dependency injection attributes by name
            var attrs = param.GetCustomAttributes().Select(a => a.GetType().Name).ToList();
            if (attrs.Contains("FromServicesAttribute") || attrs.Contains("FromKeyedServicesAttribute"))
            {
                continue;
            }

            // Check if parameter has [FromBody] by attribute name
            var isBody = attrs.Contains("FromBodyAttribute");

            // Body params are expanded by property reflection; non-body params carry their name
            // so primitive types (string, int, etc.) become named properties rather than being
            // reflected as System.String / System.Int32, which would expose Length, Chars, etc.
            yield return (param.ParameterType, isBody, isBody ? null : param.Name);
        }
    }

    /// <summary>
    /// Extracts controller name from controller type, stripping suffixes.
    /// </summary>
    private static string ExtractControllerName(Type controllerType)
    {
        var name = controllerType.Name;

        if (name.EndsWith("Controller"))
        {
            name = name[..^"Controller".Length];
        }

        if (name.EndsWith("Api") && name.Length > "Api".Length)
        {
            name = name[..^"Api".Length];
        }

        return name;
    }
}
