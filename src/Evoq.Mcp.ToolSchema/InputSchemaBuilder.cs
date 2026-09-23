#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Builds a single JSON Schema "object" (properties + required) from one or more DTO types (params and/or body).
/// In: sequence of (Type type, bool isBody) — e.g. one params type and optionally one body type.
/// Out: one object with type "object", properties (merged, camelCase keys), and required[] (from body [Required]).
/// Logic: reflect each type's properties, use JsonSchemaPropertyBuilder per property, merge; for body params collect required.
/// </summary>
public sealed class InputSchemaBuilder
{
    private readonly JsonSchemaPropertyBuilder propertyBuilder = new();
    private readonly PropertyAttributeReader attributeReader = new();

    public IReadOnlyDictionary<string, object?> Build(IEnumerable<(Type Type, bool IsBody, string? ParameterName)> parameterTypes)
    {
        var parameterTypesList = parameterTypes.ToList();
        var allProperties = new Dictionary<string, object?>();
        var requiredProperties = new List<string>();
        var constrainedJsonNames = new HashSet<string>(StringComparer.Ordinal);
        var allOfEntries = new List<Dictionary<string, object?>>();

        foreach (var (paramType, isBody, paramName) in parameterTypesList)
        {
            // Non-body primitive parameters (route/query string, int, etc.) must be added as a
            // single named property. Reflecting System.String would expose Length and Chars,
            // causing collisions when multiple string params appear on the same action.
            if (paramName != null && IsDirectlyMappableType(paramType))
            {
                var schema = JsonSchemaTypeMapper.Map(paramType);
                if (allProperties.ContainsKey(paramName))
                {
                    throw new InvalidOperationException(
                        $"Property name collision: multiple parameter types define a '{paramName}' property. " +
                        $"Unable to merge schemas when property names collide.");
                }
                allProperties[paramName] = schema;
                continue;
            }

            // Body parameters must be a request DTO with public properties. A bare primitive, enum,
            // Guid, DateTime, or collection here would otherwise be reflected for its CLR members
            // (e.g. string.Length, string.Chars, List<T>.Capacity) instead of producing a scalar or
            // array schema — a confusing, hard-to-spot mistake, so we fail fast at discovery time.
            if (isBody && IsUnsuitableBodyType(paramType))
            {
                throw new UnsupportedBodyParameterTypeException(paramType);
            }

            var properties = ExtractProperties(paramType);

            foreach (var property in properties)
            {
                var (jsonName, schema) = propertyBuilder.Build(property, property.PropertyType);

                // Route/query properties win over body: same JSON name can appear on a locator and an
                // optional body field (e.g. startIsoDate in RatePlanLocator + SaveRatePlanRequest).
                if (allProperties.ContainsKey(jsonName))
                {
                    if (isBody)
                    {
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"Property name collision: multiple parameter types define a '{jsonName}' property. " +
                        $"Unable to merge schemas when property names collide.");
                }

                allProperties[jsonName] = schema;

                // Determine if this property should be required
                var isRequired = ShouldBeRequired(property, isBody);
                if (isRequired)
                {
                    requiredProperties.Add(jsonName);
                }
            }
        }

        // Gather class-level ExactlyOneOf / AtLeastOneOf and emit oneOf/anyOf; track constrained properties so we remove them from required
        foreach (var (paramType, _, _) in parameterTypesList)
        {
            foreach (var attr in paramType.GetCustomAttributes<AtLeastOneOfAttribute>())
            {
                var jsonNames = ResolvePropertyNamesToJsonNames(paramType, attr.PropertyNames);
                if (jsonNames.Count == 0)
                {
                    continue;
                }

                foreach (var name in jsonNames)
                {
                    constrainedJsonNames.Add(name);
                }

                var anyOf = jsonNames.Select(jsonName => new Dictionary<string, object?>
                {
                    { "required", new List<string> { jsonName } }
                }).Cast<object>().ToList();
                allOfEntries.Add(new Dictionary<string, object?> { { "anyOf", anyOf } });
            }

            foreach (var attr in paramType.GetCustomAttributes<ExactlyOneOfAttribute>())
            {
                var jsonNames = ResolvePropertyNamesToJsonNames(paramType, attr.PropertyNames);
                if (jsonNames.Count == 0)
                {
                    continue;
                }

                foreach (var name in jsonNames)
                {
                    constrainedJsonNames.Add(name);
                }

                var oneOf = jsonNames.Select(jsonName => new Dictionary<string, object?>
                {
                    { "required", new List<string> { jsonName } }
                }).Cast<object>().ToList();
                allOfEntries.Add(new Dictionary<string, object?> { { "oneOf", oneOf } });
            }
        }

        // Constrained properties are not in top-level required; the allOf expresses the constraint
        var finalRequired = requiredProperties.Where(r => !constrainedJsonNames.Contains(r)).ToList();

        var result = new Dictionary<string, object?>
        {
            { "$schema", "https://json-schema.org/draft/2020-12/schema" },
            { "type", "object" },
            { "properties", new Dictionary<string, object?>(allProperties) },
            { "additionalProperties", false }
        };

        if (finalRequired.Count > 0)
        {
            result["required"] = finalRequired.AsReadOnly();
        }

        if (allOfEntries.Count > 0)
        {
            result["allOf"] = allOfEntries.Cast<object>().ToList();
        }

        return result;
    }

    /// <summary>
    /// Resolves C# property names to JSON (camelCase) names for a given type.
    /// Skips names that do not match a property on the type.
    /// </summary>
    private static IReadOnlyList<string> ResolvePropertyNamesToJsonNames(Type type, IReadOnlyList<string> propertyNames)
    {
        var result = new List<string>();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var nameToProp = props.ToDictionary(p => p.Name, StringComparer.Ordinal);

        foreach (var name in propertyNames)
        {
            if (nameToProp.TryGetValue(name, out var prop))
            {
                result.Add(GetJsonName(prop));
            }
        }

        return result;
    }

    private static string GetJsonName(PropertyInfo property)
    {
        var jsonPropAttr = property.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>();
        if (jsonPropAttr != null)
        {
            return jsonPropAttr.Name;
        }

        if (string.IsNullOrEmpty(property.Name) || char.IsLower(property.Name[0]))
        {
            return property.Name;
        }

        var chars = property.Name.ToCharArray();
        chars[0] = char.ToLowerInvariant(chars[0]);
        return new string(chars);
    }

    /// <summary>
    /// Builds schema for a complex (non-primitive) type by introspecting its properties.
    /// Used for nested objects in arrays, parameters, etc.
    /// </summary>
    public IReadOnlyDictionary<string, object?> BuildForComplexType(Type complexType)
    {
        return ComplexTypeSchemaBuilder.BuildSchema(complexType);
    }

    private static bool IsDirectlyMappableType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying.IsEnum || underlying == typeof(Guid))
        {
            return true;
        }
        var typeCode = System.Type.GetTypeCode(underlying);
        return typeCode is TypeCode.Boolean
            or TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16
            or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64
            or TypeCode.Single or TypeCode.Double or TypeCode.Decimal
            or TypeCode.String
            or TypeCode.DateTime;
    }

    /// <summary>
    /// True for types that cannot be usefully reflected as a request DTO for a body parameter:
    /// directly-mappable scalars (see <see cref="IsDirectlyMappableType"/>) plus arrays and other
    /// collection types, whose CLR members (Length, Capacity, indexers, etc.) are not the request shape.
    /// </summary>
    private static bool IsUnsuitableBodyType(Type type)
    {
        if (IsDirectlyMappableType(type))
        {
            return true;
        }

        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying.IsArray)
        {
            return true;
        }

        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(underlying) && underlying != typeof(string))
        {
            return true;
        }

        return false;
    }

    private static IReadOnlyList<PropertyInfo> ExtractProperties(Type type)
    {
        // For both record and class types, use public properties
        // Records automatically expose constructor parameters as public properties
        var publicProperties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        return publicProperties;
    }

    private bool ShouldBeRequired(PropertyInfo property, bool isBody)
    {
        var attributes = attributeReader.Read(property);

        // Only mark as required if explicitly marked with [Required] attribute
        return attributes.IsRequired;
    }
}
