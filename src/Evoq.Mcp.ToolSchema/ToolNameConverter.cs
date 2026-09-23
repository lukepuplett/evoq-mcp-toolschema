#nullable enable
using System;
using System.Reflection;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Converts controller and action names into a single snake_case tool name for MCP.
/// In: controller type name (e.g. "JournalApiController"), method name (e.g. "GetEntries").
/// Out: one string (e.g. "journal_get_entries").
/// Logic: strip "Controller" (and "Async" if present), concatenate with underscore, lower-case.
/// </summary>
public static class ToolNameConverter
{
    /// <summary>
    /// Converts controller type name and action method name to snake_case MCP tool name.
    /// Steps: strip "Controller" (and "Api" if present) and "Async" suffixes, convert PascalCase to snake_case, join with underscore.
    /// Example: JournalApiController + GetEntries => journal_get_entries
    /// </summary>
    public static string ToToolName(string controllerTypeName, string actionName)
    {
        var controllerPart = controllerTypeName;
        if (controllerPart.EndsWith("Controller"))
        {
            controllerPart = controllerPart[..^"Controller".Length];
        }

        // Also strip "Api" if it's at the end after Controller is stripped (but only if something remains)
        if (controllerPart.EndsWith("Api") && controllerPart.Length > "Api".Length)
        {
            controllerPart = controllerPart[..^"Api".Length];
        }

        var actionPart = actionName;
        if (actionPart.EndsWith("Async"))
        {
            actionPart = actionPart[..^"Async".Length];
        }

        var controllerSnake = PascalToSnakeCase(controllerPart);
        var actionSnake = PascalToSnakeCase(actionPart);

        return $"{controllerSnake}_{actionSnake}";
    }

    /// <summary>
    /// Resolves the MCP tool name for the given controller type and action method name.
    /// Checks for a [ToolName] attribute on the method first; falls back to convention via ToToolName(string, string).
    /// Use typeof + nameof at call sites for compile-time safety.
    /// Example: ToToolName(typeof(InboundTimesheetsApiController), nameof(InboundTimesheetsApiController.PerformAction))
    /// </summary>
    public static string ToToolName(Type controllerType, string actionName)
    {
        if (controllerType == null) throw new ArgumentNullException(nameof(controllerType));
        if (string.IsNullOrWhiteSpace(actionName)) throw new ArgumentException("Action name cannot be empty.", nameof(actionName));

        var method = controllerType.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);
        if (method != null)
        {
            var toolNameAttr = method.GetCustomAttribute<ToolNameAttribute>();
            if (toolNameAttr != null)
            {
                return toolNameAttr.Name;
            }
        }

        return ToToolName(controllerType.Name, actionName);
    }

    private static string PascalToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var result = new System.Text.StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c) && i > 0)
            {
                result.Append('_');
            }

            result.Append(char.ToLowerInvariant(c));
        }

        return result.ToString();
    }
}
