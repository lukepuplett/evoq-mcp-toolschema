#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Evoq.Mcp.ToolSchema;

internal static class ExampleValueCoercion
{
    public static object? Coerce(Dictionary<string, object?> schema, string? raw)
    {
        if (raw is null)
        {
            return null;
        }

        if (!schema.TryGetValue("type", out var typeValue) || typeValue is not string typeString)
        {
            return raw;
        }

        switch (typeString)
        {
            case "integer":
                if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                {
                    return l;
                }
                break;
            case "number":
                if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d))
                {
                    return d;
                }
                break;
            case "boolean":
                if (bool.TryParse(raw, out var b))
                {
                    return b;
                }
                break;
        }

        return raw;
    }

    /// <summary>
    /// Handles arrays of complex types by building the complete schema for the item type.
    /// If the schema is an array with generic items, introspects the element type and builds its full schema.
    /// </summary>
    public static Dictionary<string, object?> HandleComplexArrayType(Dictionary<string, object?> typeSchema, Type propertyType)
    {
        // Check if this is an array type
        if (typeSchema.TryGetValue("type", out var typeValue) && typeValue as string == "array")
        {
            // Get the element type
            Type? elementType = null;

            // Handle T[]
            if (propertyType.IsArray)
            {
                elementType = propertyType.GetElementType();
            }
            // Handle IEnumerable<T>, List<T>, etc.
            else if (propertyType.IsGenericType)
            {
                var genericDef = propertyType.GetGenericTypeDefinition();
                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyType))
                {
                    var args = propertyType.GetGenericArguments();
                    if (args.Length > 0)
                    {
                        elementType = args[0];
                    }
                }
            }

            // If we found an element type and it's a complex type (not primitive, not enum)
            if (elementType != null && IsComplexType(elementType))
            {
                var itemSchema = ComplexTypeSchemaBuilder.BuildSchema(elementType);
                if (typeSchema.ContainsKey("items"))
                {
                    typeSchema["items"] = itemSchema;
                }
            }
        }

        return typeSchema;
    }

    /// <summary>
    /// Determines if a type is a complex type (not primitive, not enum, not built-in collection).
    /// </summary>
    private static bool IsComplexType(Type type)
    {
        // Unwrap nullable
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // Primitives and enums are not complex
        if (underlyingType.IsEnum)
        {
            return false;
        }

        var typeCode = System.Type.GetTypeCode(underlyingType);
        if (typeCode != TypeCode.Object)
        {
            return false; // It's a primitive
        }

        // Check for special types that are not complex
        if (underlyingType == typeof(Guid) || underlyingType == typeof(decimal) || underlyingType == typeof(DateTime))
        {
            return false;
        }

        // Everything else (classes, records, structs) is complex
        return true;
    }

    private static string GetJsonPropertyName(PropertyInfo property)
    {
        // Check for [JsonPropertyName] attribute
        var jsonPropAttr = property.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>();
        if (jsonPropAttr != null)
        {
            return jsonPropAttr.Name;
        }

        // Convert property name from PascalCase to camelCase
        return ConvertToCamelCase(property.Name);
    }

    private static string ConvertToCamelCase(string pascalCaseName)
    {
        if (string.IsNullOrEmpty(pascalCaseName) || char.IsLower(pascalCaseName[0]))
        {
            return pascalCaseName;
        }

        var chars = pascalCaseName.ToCharArray();
        chars[0] = char.ToLowerInvariant(chars[0]);
        return new string(chars);
    }
}
