#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Linq;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Marks a controller or action method as belonging to one or more MCP surface areas.
/// Optionally restricts the surface to specific communication channels.
/// Applied at controller-level: all actions in that controller belong to the surface(s).
/// Applied at action-level: only that action belongs to the surface(s); additive with controller-level.
///
/// Examples:
/// [BotSurface("journal")] — available on all channels
/// [BotSurface("journal", channel: "MCP")] — available only on MCP channel
/// [BotSurface("journal", channel: "WhatsApp")] — available only on WhatsApp channel
/// Use repeated attributes for multiple channels:
/// [BotSurface("journal", channel: "MCP")]
/// [BotSurface("journal", channel: "WhatsApp")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class BotSurfaceAttribute : Attribute
{
    /// <summary>
    /// One or more surface names this controller/action belongs to.
    /// </summary>
    public IReadOnlyCollection<string> Surfaces { get; }

    /// <summary>
    /// Optional channel name restricting this surface to a specific communication channel.
    /// If null, the surface is available on all channels.
    /// </summary>
    public string? Channel { get; }

    /// <summary>
    /// Marks a controller or action as belonging to one or more MCP surface areas.
    /// Available on all channels.
    /// </summary>
    public BotSurfaceAttribute(params string[] surfaces)
    {
        if (surfaces == null || surfaces.Length == 0)
        {
            throw new ArgumentException("At least one surface name must be specified", nameof(surfaces));
        }

        this.Surfaces = Array.AsReadOnly(surfaces);
        this.Channel = null;
    }

    /// <summary>
    /// Marks a controller or action as belonging to a surface, restricted to a specific channel.
    /// </summary>
    public BotSurfaceAttribute(string surfaceName, string channel)
    {
        if (string.IsNullOrWhiteSpace(surfaceName))
        {
            throw new ArgumentException("Surface name cannot be null or empty", nameof(surfaceName));
        }

        if (string.IsNullOrWhiteSpace(channel))
        {
            throw new ArgumentException("Channel name cannot be null or empty", nameof(channel));
        }

        this.Surfaces = Array.AsReadOnly(new[] { surfaceName });
        this.Channel = channel;
    }
}

/// <summary>
/// Marks a controller or action method as excluded from MCP tool discovery.
/// Applied at controller-level: all actions in that controller are excluded.
/// Applied at action-level: only that action is excluded.
/// Use this to hide internal or unsafe endpoints from tool exposure.
/// Example: [ExcludeFromTools] or [ExcludeFromTools("This endpoint requires direct authentication")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ExcludeFromToolsAttribute : Attribute
{
    /// <summary>
    /// Optional reason for exclusion (for documentation/code review purposes).
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Marks a controller or action as excluded from MCP tool discovery.
    /// </summary>
    public ExcludeFromToolsAttribute(string? reason = null)
    {
        this.Reason = reason;
    }
}

/// <summary>
/// Declares MCP tool safety hint annotations for an action method.
/// Emitted in the tools/list response as the "annotations" object per the MCP 2025-03-26 spec.
///
/// Use this only to override the convention-based defaults applied by McpToolDefinitionBuilder:
///   HttpGet  → readOnly=true,  destructive=false, openWorld=false
///   HttpDelete → readOnly=false, destructive=true,  openWorld=false
///   All other HTTP methods → all false
///
/// Examples where an explicit override is needed:
///   HttpPost that permanently deletes:  [ToolHints(destructive: true)]
///   HttpGet that calls an external API: [ToolHints(openWorld: true)]
///   HttpPost that sends a notification: [ToolHints(openWorld: true)]
///   HttpPost that deletes AND notifies: [ToolHints(destructive: true, openWorld: true)]
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ToolHintsAttribute : Attribute
{
    public bool ReadOnly { get; }
    public bool Destructive { get; }
    public bool OpenWorld { get; }
    public bool Idempotent { get; }

    public ToolHintsAttribute(
        bool readOnly = false,
        bool destructive = false,
        bool openWorld = false,
        bool idempotent = false)
    {
        this.ReadOnly = readOnly;
        this.Destructive = destructive;
        this.OpenWorld = openWorld;
        this.Idempotent = idempotent;
    }
}

/// <summary>
/// Overrides the convention-derived MCP tool name for an action method.
/// When absent, the tool name is derived from the controller and action names via ToolNameConverter.
/// When present, the specified name is used verbatim (must be a valid snake_case MCP tool name).
/// Example: [ToolName("account_submit_feedback")]
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ToolNameAttribute : Attribute
{
    /// <summary>
    /// The exact MCP tool name to use for this action.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Overrides the convention-derived tool name with the specified value.
    /// </summary>
    public ToolNameAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tool name cannot be null or empty.", nameof(name));
        }

        this.Name = name;
    }
}

/// <summary>
/// Exactly one of the listed properties must be set.
/// Emitted in JSON Schema as oneOf (exactly one of the subschemas valid).
/// Apply at class level. Property names are C# names; schema builder resolves to JSON (camelCase) names.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ExactlyOneOfAttribute : Attribute
{
    /// <summary>
    /// C# property names; exactly one must be present/set.
    /// </summary>
    public IReadOnlyList<string> PropertyNames { get; }

    /// <summary>
    /// Exactly one of the given properties must be provided.
    /// </summary>
    public ExactlyOneOfAttribute(params string[] propertyNames)
    {
        if (propertyNames == null || propertyNames.Length < 2)
        {
            throw new ArgumentException("At least two property names must be specified for ExactlyOneOf", nameof(propertyNames));
        }

        this.PropertyNames = propertyNames.ToList().AsReadOnly();
    }
}

/// <summary>
/// At least one of the listed properties must be set.
/// Emitted in JSON Schema as anyOf.
/// Apply at class level. Property names are C# names; schema builder resolves to JSON (camelCase) names.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AtLeastOneOfAttribute : Attribute
{
    /// <summary>
    /// C# property names; at least one must be present/set.
    /// </summary>
    public IReadOnlyList<string> PropertyNames { get; }

    /// <summary>
    /// At least one of the given properties must be provided.
    /// </summary>
    public AtLeastOneOfAttribute(params string[] propertyNames)
    {
        if (propertyNames == null || propertyNames.Length < 2)
        {
            throw new ArgumentException("At least two property names must be specified for AtLeastOneOf", nameof(propertyNames));
        }

        this.PropertyNames = propertyNames.ToList().AsReadOnly();
    }
}

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

// -----------------------------------------------------------------------------
// MCP tool schema generation: small, testable pieces that turn reflected
// API action methods and their param/body types into MCP-style tool definitions
// (name, description, inputSchema). Each type below is a stub for future implementation.
// -----------------------------------------------------------------------------

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

/// <summary>
/// Reads attribute metadata from a property or parameter for use in schema generation.
/// In: MemberInfo (e.g. PropertyInfo or ParameterInfo).
/// Out: description, examples, isRequired, min/max, maxLength, pattern, etc. (e.g. a small DTO or record).
/// Logic: GetCustomAttribute for Description, Required, Range, Example, MaxLength, RegularExpression, etc.
/// </summary>
public sealed class PropertyAttributeReader
{
    public PropertyAttributeBag Read(MemberInfo member)
    {
        // Read [Description] from System.ComponentModel
        var descriptionAttr = GetAttributeWithConstructorFallback<System.ComponentModel.DescriptionAttribute>(member);
        var description = descriptionAttr?.Description;

        // Collect all [Example] attributes (may be custom attribute)
        var exampleList = new List<string?>();
        foreach (var attr in GetAttributesWithConstructorFallback(member, "ExampleAttribute"))
        {
            // Try to get the Value property (common convention)
            var valueProp = attr.GetType().GetProperty("Value");
            if (valueProp != null)
            {
                var value = valueProp.GetValue(attr)?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    exampleList.Add(value);
                }
            }
        }

        // Read [Required] from the member (property or parameter).
        // For C# records, attributes on a primary-constructor parameter are not copied to the
        // synthesized property (unless targeted with `[property: ...]`), so every attribute read
        // here falls back to the matching constructor parameter when member is a PropertyInfo.
        var isRequired = GetAttributeWithConstructorFallback<System.ComponentModel.DataAnnotations.RequiredAttribute>(member) != null;

        // Read [Range(min, max)]
        var rangeAttr = GetAttributeWithConstructorFallback<System.ComponentModel.DataAnnotations.RangeAttribute>(member);
        double? minimum = null;
        double? maximum = null;
        if (rangeAttr != null)
        {
            if (rangeAttr.Minimum != null && double.TryParse(rangeAttr.Minimum.ToString(), out var minVal))
            {
                minimum = minVal;
            }

            if (rangeAttr.Maximum != null && double.TryParse(rangeAttr.Maximum.ToString(), out var maxVal))
            {
                maximum = maxVal;
            }
        }

        // Read [MaxLength(n)]
        var maxLengthAttr = GetAttributeWithConstructorFallback<System.ComponentModel.DataAnnotations.MaxLengthAttribute>(member);
        int? maxLength = maxLengthAttr?.Length;

        // Read [RegularExpression(...)]
        var regexAttr = GetAttributeWithConstructorFallback<System.ComponentModel.DataAnnotations.RegularExpressionAttribute>(member);
        var pattern = regexAttr?.Pattern;

        // Read [AllowedValues(...)] (ASP.NET / Evoq attribute) for enum-like constraints
        // without changing the underlying CLR type to an actual enum.
        IReadOnlyList<string>? enumValues = null;
        var allowedValuesAttr = GetAttributesWithConstructorFallback(member, "AllowedValuesAttribute").FirstOrDefault();

        if (allowedValuesAttr != null)
        {
            var valuesProp = allowedValuesAttr.GetType().GetProperty("Values");
            if (valuesProp != null)
            {
                if (valuesProp.GetValue(allowedValuesAttr) is System.Collections.IEnumerable enumerable)
                {
                    var values = enumerable.Cast<object?>()
                        .Select(v => v?.ToString())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Cast<string>()
                        .ToList();

                    if (values.Count > 0)
                    {
                        enumValues = values.AsReadOnly();
                    }
                }
            }
        }

        return new PropertyAttributeBag(
            Description: description,
            Examples: exampleList.Count > 0 ? exampleList.AsReadOnly() : null,
            IsRequired: isRequired,
            Minimum: minimum,
            Maximum: maximum,
            MaxLength: maxLength,
            Pattern: pattern,
            EnumValues: enumValues);
    }

    /// <summary>
    /// Reads a strongly-typed attribute from a member, falling back to the matching primary-constructor
    /// parameter (by name) for record properties whose attributes weren't `[property: ...]`-targeted.
    /// </summary>
    private static TAttribute? GetAttributeWithConstructorFallback<TAttribute>(MemberInfo member)
        where TAttribute : Attribute
    {
        var direct = member.GetCustomAttribute<TAttribute>();
        if (direct != null)
        {
            return direct;
        }

        return FindMatchingConstructorParameter(member)?.GetCustomAttribute<TAttribute>();
    }

    /// <summary>
    /// Reads attributes matched by type name (for attributes not referenceable by compile-time type),
    /// falling back to the matching primary-constructor parameter as above.
    /// </summary>
    private static IEnumerable<Attribute> GetAttributesWithConstructorFallback(MemberInfo member, string attributeTypeName)
    {
        var direct = member.GetCustomAttributes()
            .Where(a => string.Equals(a.GetType().Name, attributeTypeName, StringComparison.Ordinal))
            .ToList();

        if (direct.Count > 0)
        {
            return direct;
        }

        var param = FindMatchingConstructorParameter(member);
        if (param == null)
        {
            return Enumerable.Empty<Attribute>();
        }

        return param.GetCustomAttributes()
            .Where(a => string.Equals(a.GetType().Name, attributeTypeName, StringComparison.Ordinal));
    }

    private static ParameterInfo? FindMatchingConstructorParameter(MemberInfo member)
    {
        if (member is not PropertyInfo propInfo)
        {
            return null;
        }

        var declaringType = propInfo.DeclaringType;
        if (declaringType == null)
        {
            return null;
        }

        foreach (var ctor in declaringType.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
        {
            var param = ctor.GetParameters().FirstOrDefault(p => string.Equals(p.Name, propInfo.Name, StringComparison.Ordinal));
            if (param != null)
            {
                return param;
            }
        }

        return null;
    }
}

/// <summary>
/// Bag of attribute-derived values for one property/parameter (output of PropertyAttributeReader).
/// </summary>
public sealed record PropertyAttributeBag(
    string? Description,
    IReadOnlyList<string?>? Examples,
    bool IsRequired,
    double? Minimum,
    double? Maximum,
    int? MaxLength,
    string? Pattern,
    IReadOnlyList<string>? EnumValues);

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

/// <summary>
/// Thrown when a [FromBody] action parameter is a primitive, enum, Guid, DateTime, or bare collection
/// type rather than a request DTO. Schema generation reflects the body type's public properties to
/// build the tool's inputSchema; a primitive exposes irrelevant CLR members (e.g. string.Length,
/// string.Chars) instead of a scalar value, and a bare collection exposes List&lt;T&gt;/array members
/// instead of an array schema. Proper model binding — a class or record DTO — is required for body
/// parameters so the generated schema reflects the actual request shape.
/// </summary>
public sealed class UnsupportedBodyParameterTypeException : Exception
{
    /// <summary>
    /// The offending body parameter's CLR type.
    /// </summary>
    public Type ParameterType { get; }

    public UnsupportedBodyParameterTypeException(Type parameterType)
        : base(
            $"Unable to build MCP input schema for body parameter of type '{parameterType.Name}'. " +
            "[FromBody] parameters must be a request DTO (a class or record with public properties), " +
            "not a primitive, enum, Guid, DateTime, or bare collection type. Wrap the value in a request " +
            $"type, e.g. 'public sealed record {parameterType.Name}Request({parameterType.Name} Value);', and bind that instead.")
    {
        this.ParameterType = parameterType;
    }
}

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

/// <summary>
/// Metadata for one discovered tool: everything needed to generate its schema or invoke it.
/// </summary>
public readonly record struct ToolRegistration(
    Type ControllerType,
    MethodInfo ActionMethod,
    IReadOnlyList<(Type Type, bool IsBody)> ParameterTypes);
