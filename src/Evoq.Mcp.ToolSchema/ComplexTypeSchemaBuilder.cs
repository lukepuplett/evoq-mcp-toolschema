#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Builds complete JSON Schema for complex (non-primitive) types by introspecting all properties.
/// Recursively handles nested objects.
/// In: Type (e.g. WorkflowStep)
/// Out: full object schema with all properties, descriptions, types, required fields, etc.
/// </summary>
public static class ComplexTypeSchemaBuilder
{
    private static readonly PropertyAttributeReader attributeReader = new();

    public static IReadOnlyDictionary<string, object?> BuildSchema(Type complexType)
    {
        var properties = new Dictionary<string, object?>();
        var requiredFields = new List<string>();

        // Get all public properties from the type
        var publicProperties = complexType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var property in publicProperties)
        {
            // Build schema for this property using JsonSchemaTypeMapper
            var jsonName = GetJsonPropertyName(property);
            var typeSchema = new Dictionary<string, object?>(JsonSchemaTypeMapper.Map(property.PropertyType));

            // Get attribute metadata
            var attributes = attributeReader.Read(property);

            // Merge attributes into schema
            if (!string.IsNullOrEmpty(attributes.Description))
            {
                typeSchema["description"] = attributes.Description;
            }

            if (attributes.Examples != null && attributes.Examples.Count > 0)
            {
                // Use "example" (singular) for single example, "examples" (plural) for multiple.
                // When the target schema type is numeric or boolean, attempt to coerce the example
                // string into the appropriate JSON-native type so tools see correct types.
                if (attributes.Examples.Count == 1)
                {
                    typeSchema["example"] = ExampleValueCoercion.Coerce(typeSchema, attributes.Examples[0]);
                }
                else
                {
                    var coerced = attributes.Examples
                        .Select(e => ExampleValueCoercion.Coerce(typeSchema, e))
                        .ToList();
                    typeSchema["examples"] = coerced;
                }
            }

            if (attributes.Minimum.HasValue)
            {
                typeSchema["minimum"] = attributes.Minimum.Value;
            }

            if (attributes.Maximum.HasValue)
            {
                typeSchema["maximum"] = attributes.Maximum.Value;
            }

            if (attributes.MaxLength.HasValue)
            {
                typeSchema["maxLength"] = attributes.MaxLength.Value;
            }

            if (!string.IsNullOrEmpty(attributes.Pattern))
            {
                typeSchema["pattern"] = attributes.Pattern;
            }

            if (attributes.EnumValues != null && attributes.EnumValues.Count > 0)
            {
                typeSchema["enum"] = attributes.EnumValues;
            }

            properties[jsonName] = typeSchema;

            // Check if property is required
            if (attributes.IsRequired)
            {
                requiredFields.Add(jsonName);
            }
        }

        var result = new Dictionary<string, object?>
        {
            { "type", "object" },
            { "properties", new Dictionary<string, object?>(properties) },
            { "additionalProperties", false }
        };

        if (requiredFields.Count > 0)
        {
            result["required"] = requiredFields.AsReadOnly();
        }

        return result;
    }

    private static string GetJsonPropertyName(System.Reflection.PropertyInfo property)
    {
        // Check for [JsonPropertyName] attribute
        var jsonPropAttr = property.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>();
        if (jsonPropAttr != null)
        {
            return jsonPropAttr.Name;
        }

        // Convert property name from PascalCase to camelCase
        if (string.IsNullOrEmpty(property.Name) || char.IsLower(property.Name[0]))
        {
            return property.Name;
        }

        var chars = property.Name.ToCharArray();
        chars[0] = char.ToLowerInvariant(chars[0]);
        return new string(chars);
    }
}
