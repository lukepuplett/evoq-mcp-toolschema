#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Finds action methods on a controller that qualify for MCP exposure (e.g. have [Http*] and [Description]).
/// In: controller Type.
/// Out: list of (MethodInfo method, string controllerName, string actionName) for each qualifying action.
/// Logic: get public methods, filter by presence of HTTP method attribute AND Description attribute.
/// </summary>
public sealed class QualifyingActionDiscoverer
{
    /// <summary>
    /// Discovers all action methods on a controller that qualify for MCP tool generation WITHOUT surface filtering.
    /// A method qualifies if it has BOTH an [Http*] attribute AND a [Description] attribute.
    /// Methods marked with [Obsolete] are excluded.
    /// NOTE: This discovers everything regardless of [BotSurface]. Use DiscoverBySurface() for filtered discovery.
    /// </summary>
    [Obsolete("Use DiscoverBySurface(controllerType, surfaceName) instead. This method discovers everything, which is not safe for MCP.")]
    public IReadOnlyList<(MethodInfo Method, string ControllerName, string ActionName)> Discover(Type controllerType)
    {
        var results = new List<(MethodInfo, string, string)>();

        // Get the controller name (may have "Controller" suffix stripped)
        var controllerName = ExtractControllerName(controllerType);

        // Get all public instance methods
        var methods = controllerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            // Check for [Obsolete] attribute - skip obsolete methods
            var obsoleteAttr = method.GetCustomAttribute<System.ObsoleteAttribute>();
            if (obsoleteAttr != null)
            {
                continue;
            }

            // Check for [ExcludeFromTools] at action or controller level
            if (method.GetCustomAttribute<ExcludeFromToolsAttribute>() != null || controllerType.GetCustomAttribute<ExcludeFromToolsAttribute>() != null)
            {
                continue;
            }

            // Check for [Description] attribute
            var descriptionAttr = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            if (descriptionAttr == null)
            {
                continue;
            }

            // Check for [Http*] attribute
            if (!HasHttpMethodAttribute(method))
            {
                continue;
            }

            // Extract action name (optionally strip "Async")
            var actionName = method.Name;

            results.Add((method, controllerName, actionName));
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Discovers all action methods on a controller that qualify for MCP tool generation AND belong to the specified surface.
    /// A method qualifies if it has:
    /// 1. BOTH an [Http*] attribute AND a [Description] attribute (two-pot intersection)
    /// 2. [BotSurface] attribute (at controller or action level) containing the specified surfaceName
    /// 3. NOT marked with [Obsolete]
    /// Returns empty list if no actions match the surface.
    /// </summary>
    public IReadOnlyList<(MethodInfo Method, string ControllerName, string ActionName)> DiscoverBySurface(Type controllerType, string surfaceName, string? channelName = null)
    {
        if (string.IsNullOrWhiteSpace(surfaceName))
        {
            throw new ArgumentException("Surface name cannot be null or empty", nameof(surfaceName));
        }

        var results = new List<(MethodInfo, string, string)>();

        // Get the controller name (may have "Controller" suffix stripped)
        var controllerName = ExtractControllerName(controllerType);

        // Check if controller has [BotSurface] attribute(s)
        var controllerSurfaceAttrs = controllerType.GetCustomAttributes<BotSurfaceAttribute>();
        var controllerSurfaces = controllerSurfaceAttrs.Select(a => a.Surfaces).SelectMany(s => s).ToList();
        var controllerChannelRestrictions = controllerSurfaceAttrs
            .Where(a => a.Channel != null)
            .GroupBy(a => a.Surfaces.First())
            .ToDictionary(g => g.Key, g => g.Select(a => a.Channel!).ToList());

        // Get all public instance methods
        var methods = controllerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            // Check for [Obsolete] attribute - skip obsolete methods
            var obsoleteAttr = method.GetCustomAttribute<System.ObsoleteAttribute>();
            if (obsoleteAttr != null)
            {
                continue;
            }

            // Check for [ExcludeFromTools] at action level
            var actionExcludeAttr = method.GetCustomAttribute<ExcludeFromToolsAttribute>();
            if (actionExcludeAttr != null)
            {
                continue;
            }

            // Check for [ExcludeFromTools] at controller level (unless overridden at action level)
            if (actionExcludeAttr == null)
            {
                var controllerExcludeAttr = controllerType.GetCustomAttribute<ExcludeFromToolsAttribute>();
                if (controllerExcludeAttr != null)
                {
                    continue;
                }
            }

            // Check for [Description] attribute (two-pot requirement)
            var descriptionAttr = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            if (descriptionAttr == null)
            {
                continue;
            }

            // Check for [Http*] attribute (two-pot requirement)
            if (!HasHttpMethodAttribute(method))
            {
                continue;
            }

            // Check for [BotSurface] at action level (action-level overrides controller-level)
            var actionSurfaceAttrs = method.GetCustomAttributes<BotSurfaceAttribute>();

            if (actionSurfaceAttrs.Count() > 0)
            {
                // Action has explicit surface attribute(s) - use only action-level
                var actionSurfaces = actionSurfaceAttrs.Select(a => a.Surfaces).SelectMany(s => s).ToList();
                var actionChannelRestrictions = actionSurfaceAttrs
                    .Where(a => a.Channel != null)
                    .GroupBy(a => a.Surfaces.First())
                    .ToDictionary(g => g.Key, g => g.Select(a => a.Channel!).ToList());

                if (!actionSurfaces.Contains(surfaceName, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Check channel restriction if specified
                if (channelName != null && actionChannelRestrictions.TryGetValue(surfaceName, out var allowedChannels))
                {
                    if (!allowedChannels.Contains(channelName, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }
            }
            else
            {
                // No action-level surface attribute - use controller-level
                if (!controllerSurfaces.Contains(surfaceName, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Check channel restriction if specified
                if (channelName != null && controllerChannelRestrictions.TryGetValue(surfaceName, out var allowedChannels))
                {
                    if (!allowedChannels.Contains(channelName, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }
            }

            // Extract action name (optionally strip "Async")
            var actionName = method.Name;

            results.Add((method, controllerName, actionName));
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// Extracts controller name from controller type, optionally stripping "Controller" and "Api" suffixes.
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

    /// <summary>
    /// Checks if a method has any [Http*] attribute ([HttpGet], [HttpPost], etc.).
    /// </summary>
    private static bool HasHttpMethodAttribute(MethodInfo method)
    {
        // Check for specific HTTP method attributes by name
        var httpAttributeNames = new[]
        {
            "HttpGetAttribute",
            "HttpPostAttribute",
            "HttpPutAttribute",
            "HttpDeleteAttribute",
            "HttpPatchAttribute",
            "HttpHeadAttribute",
            "HttpOptionsAttribute"
        };

        var attributes = method.GetCustomAttributes();
        return attributes.Any(attr => httpAttributeNames.Contains(attr.GetType().Name));
    }
}
