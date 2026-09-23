#nullable enable
using System;
using System.Collections.Generic;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Maps a .NET type to a JSON Schema type (and optional constraints).
/// In: System.Type (e.g. int?, string, bool, string[]).
/// Out: a dictionary representing one JSON Schema type (e.g. type + minimum/maximum, or type + items for arrays).
/// Logic: map primitives and nullable; handle arrays and nested objects; handle enums.
/// </summary>
public static class JsonSchemaTypeMapper
{
    public static IReadOnlyDictionary<string, object?> Map(Type type)
    {
        // Fail fast on null input: callers should ensure non-null types are provided
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type), "Unable to map schema for null type. Type must be non-null.");
        }

        // Handle nullable types: extract underlying type
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // Handle arrays (T[])
        if (underlyingType.IsArray)
        {
            var elementType = underlyingType.GetElementType()!;
            var itemsSchema = Map(elementType);
            return new Dictionary<string, object?> { { "type", "array" }, { "items", itemsSchema } };
        }

        // Handle generic collections (List<T>, IEnumerable<T>, etc.)
        if (underlyingType.IsGenericType)
        {
            var genericDef = underlyingType.GetGenericTypeDefinition();
            var genericArgs = underlyingType.GetGenericArguments();

            // List<T>, IList<T>, IEnumerable<T>, ICollection<T>
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(underlyingType))
            {
                if (genericArgs.Length == 1)
                {
                    var itemType = genericArgs[0];
                    var itemsSchema = Map(itemType);
                    return new Dictionary<string, object?> { { "type", "array" }, { "items", itemsSchema } };
                }
            }
        }

        // Handle enum types
        if (underlyingType.IsEnum)
        {
            var enumValues = System.Enum.GetNames(underlyingType);
            var result = new Dictionary<string, object?> { { "type", "string" }, { "enum", enumValues } };
            return result;
        }

        // Handle primitive types
        var typeCode = System.Type.GetTypeCode(underlyingType);
        return typeCode switch
        {
            TypeCode.Boolean => new Dictionary<string, object?> { { "type", "boolean" } },
            TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 =>
                new Dictionary<string, object?> { { "type", "integer" } },
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal =>
                new Dictionary<string, object?> { { "type", "number" } },
            TypeCode.String => new Dictionary<string, object?> { { "type", "string" } },
            TypeCode.DateTime => new Dictionary<string, object?> { { "type", "string" }, { "format", "date-time" } },
            _ => HandleSpecialTypes(underlyingType)
        };
    }

    private static IReadOnlyDictionary<string, object?> HandleSpecialTypes(Type type)
    {
        // Handle Guid
        if (type == typeof(Guid))
        {
            return new Dictionary<string, object?> { { "type", "string" }, { "format", "uuid" } };
        }

        // Handle decimal (if not caught by TypeCode)
        if (type == typeof(decimal))
        {
            return new Dictionary<string, object?> { { "type", "number" } };
        }

        // Default for unknown types: treat as object
        return new Dictionary<string, object?> { { "type", "object" } };
    }
}
