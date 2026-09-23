#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Evoq.Mcp.ToolSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Evoq.Mcp.ToolSchema.Tests;

[TestClass]
public class JsonSchemaTypeMapperTests
{
    // Primitive integer types
    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__int__then__type_integer()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(int));

        Assert.AreEqual("integer", result["type"], "int should map to integer");
        Assert.AreEqual(1, result.Count, "Should only have type property");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__nullable_int__then__type_integer()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(int?));

        Assert.AreEqual("integer", result["type"], "int? should map to integer");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__long__then__type_integer()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(long));

        Assert.AreEqual("integer", result["type"], "long should map to integer");
    }

    // Number types
    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__double__then__type_number()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(double));

        Assert.AreEqual("number", result["type"], "double should map to number");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__decimal__then__type_number()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(decimal));

        Assert.AreEqual("number", result["type"], "decimal should map to number");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__float__then__type_number()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(float));

        Assert.AreEqual("number", result["type"], "float should map to number");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__nullable_decimal__then__type_number()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(decimal?));

        Assert.AreEqual("number", result["type"], "decimal? should map to number");
    }

    // Boolean
    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__bool__then__type_boolean()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(bool));

        Assert.AreEqual("boolean", result["type"], "bool should map to boolean");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__nullable_bool__then__type_boolean()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(bool?));

        Assert.AreEqual("boolean", result["type"], "bool? should map to boolean");
    }

    // String
    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__string__then__type_string()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(string));

        Assert.AreEqual("string", result["type"], "string should map to string");
    }

    // DateTime
    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__datetime__then__type_string_format_datetime()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(System.DateTime));

        Assert.AreEqual("string", result["type"], "DateTime should have type string");
        Assert.AreEqual("date-time", result["format"], "DateTime should have format date-time");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__nullable_datetime__then__type_string_format_datetime()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(System.DateTime?));

        Assert.AreEqual("string", result["type"], "DateTime? should have type string");
        Assert.AreEqual("date-time", result["format"], "DateTime? should have format date-time");
    }

    // Guid
    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__guid__then__type_string_format_uuid()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(System.Guid));

        Assert.AreEqual("string", result["type"], "Guid should have type string");
        Assert.AreEqual("uuid", result["format"], "Guid should have format uuid");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__nullable_guid__then__type_string_format_uuid()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(System.Guid?));

        Assert.AreEqual("string", result["type"], "Guid? should have type string");
        Assert.AreEqual("uuid", result["format"], "Guid? should have format uuid");
    }

    // Arrays
    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__string_array__then__type_array_items_string()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(string[]));

        Assert.AreEqual("array", result["type"], "string[] should have type array");
        Assert.IsTrue(result.ContainsKey("items"), "array should have items property");

        var items = result["items"] as IReadOnlyDictionary<string, object?>;
        Assert.IsNotNull(items, "items should be a dictionary");
        Assert.AreEqual("string", items?["type"], "array items should have type string");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__int_array__then__type_array_items_integer()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(int[]));

        Assert.AreEqual("array", result["type"], "int[] should have type array");
        var items = result["items"] as IReadOnlyDictionary<string, object?>;
        Assert.AreEqual("integer", items?["type"], "array items should have type integer");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__list_of_string__then__type_array_items_string()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(List<string>));

        Assert.AreEqual("array", result["type"], "List<string> should have type array");
        var items = result["items"] as IReadOnlyDictionary<string, object?>;
        Assert.AreEqual("string", items?["type"], "array items should have type string");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__list_of_int__then__type_array_items_integer()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(List<int>));

        Assert.AreEqual("array", result["type"], "List<int> should have type array");
        var items = result["items"] as IReadOnlyDictionary<string, object?>;
        Assert.AreEqual("integer", items?["type"], "array items should have type integer");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__list_of_decimal__then__type_array_items_number()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(List<decimal>));

        Assert.AreEqual("array", result["type"], "List<decimal> should have type array");
        var items = result["items"] as IReadOnlyDictionary<string, object?>;
        Assert.AreEqual("number", items?["type"], "array items should have type number");
    }

    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__enumerable_of_string__then__type_array_items_string()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(System.Collections.Generic.IEnumerable<string>));

        Assert.AreEqual("array", result["type"], "IEnumerable<string> should have type array");
        var items = result["items"] as IReadOnlyDictionary<string, object?>;
        Assert.AreEqual("string", items?["type"], "array items should have type string");
    }

    // Enums
    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__enum_type__then__type_string_with_enum_values()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(TestColorEnum));

        Assert.AreEqual("string", result["type"], "Enum should have type string");
        Assert.IsTrue(result.ContainsKey("enum"), "Enum should have enum property");

        var enumValues = result["enum"] as string[];
        Assert.IsNotNull(enumValues, "enum values should be string array");
        Assert.AreEqual(3, enumValues?.Length, "should have 3 enum values");
        Assert.IsTrue(enumValues?.Contains("Red") ?? false, "should contain Red");
        Assert.IsTrue(enumValues?.Contains("Green") ?? false, "should contain Green");
        Assert.IsTrue(enumValues?.Contains("Blue") ?? false, "should contain Blue");
    }

    // Complex types (default to object)
    [TestMethod]
    public void JsonSchemaTypeMapper__Map__when__custom_class__then__type_object()
    {
        var result = JsonSchemaTypeMapper.Map(typeof(TestClass));

        Assert.AreEqual("object", result["type"], "Custom class should map to object");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void JsonSchemaTypeMapper__Map__when__null_type__then__throws_argument_null_exception()
    {
        // Calling Map with null should throw ArgumentNullException
        // Fail-fast design: callers must provide non-null types
        JsonSchemaTypeMapper.Map(null!);

        // If we get here, the test fails
        Assert.Fail("Map(null) should throw ArgumentNullException");
    }

    // Helper types for testing
    private enum TestColorEnum
    {
        Red,
        Green,
        Blue
    }

    private class TestClass
    {
        public string Name { get; set; } = string.Empty;
    }
}
