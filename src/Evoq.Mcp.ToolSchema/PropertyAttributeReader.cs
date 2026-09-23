#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evoq.Mcp.ToolSchema;

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
