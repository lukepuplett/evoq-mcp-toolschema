#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Builds one JSON Schema property object for a single .NET property.
/// In: PropertyInfo (or equivalent for record parameters), and the property's Type.
/// Out: one JSON Schema property (type, description?, default?, minimum?, examples?, etc.) keyed by camelCase name.
/// Logic: use JsonSchemaTypeMapper for type, PropertyAttributeReader for attributes; combine; produce camelCase key.
/// For arrays of complex types, recursively builds the item schema.
/// </summary>
public sealed class JsonSchemaPropertyBuilder
{
    private readonly PropertyAttributeReader attributeReader = new();

    public (string JsonName, IReadOnlyDictionary<string, object?> Schema) Build(PropertyInfo property, Type propertyType)
    {
        // Get the JSON property name (check for [JsonPropertyName] first, otherwise convert to camelCase)
        var jsonName = GetJsonPropertyName(property);

        // Get the base type schema from JsonSchemaTypeMapper
        var typeSchema = new Dictionary<string, object?>(JsonSchemaTypeMapper.Map(propertyType));

        // Special handling: if this is an array of complex objects, build the item schema recursively
        typeSchema = HandleComplexArrayType(typeSchema, propertyType);

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

        return (jsonName, typeSchema);
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

    private static Dictionary<string, object?> HandleComplexArrayType(Dictionary<string, object?> typeSchema, Type propertyType)
    {
        return ExampleValueCoercion.HandleComplexArrayType(typeSchema, propertyType);
    }
}
