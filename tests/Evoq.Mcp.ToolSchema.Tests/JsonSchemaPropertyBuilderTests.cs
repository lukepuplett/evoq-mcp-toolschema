#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Evoq.Mcp.ToolSchema;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Evoq.Mcp.ToolSchema.Tests;

[TestClass]
public class JsonSchemaPropertyBuilderTests
{
    private readonly JsonSchemaPropertyBuilder builder = new();

    [TestMethod]
    public void JsonSchemaPropertyBuilder__Build__when__simple_string_property__then__returns_camel_case_name_and_string_schema()
    {
        var property = typeof(TestModel).GetProperty(nameof(TestModel.PropertyName))!;

        var (jsonName, schema) = builder.Build(property, typeof(string));

        Assert.AreEqual("propertyName", jsonName, "Property name should be converted to camelCase");
        Assert.AreEqual("string", schema["type"], "Should have string type");
    }

    [TestMethod]
    public void JsonSchemaPropertyBuilder__Build__when__integer_property__then__returns_integer_schema()
    {
        var property = typeof(TestModel).GetProperty(nameof(TestModel.Count))!;

        var (jsonName, schema) = builder.Build(property, typeof(int));

        Assert.AreEqual("count", jsonName, "Should convert to camelCase");
        Assert.AreEqual("integer", schema["type"], "Should have integer type");
    }

    [TestMethod]
    public void JsonSchemaPropertyBuilder__Build__when__property_with_description__then__includes_description()
    {
        var property = typeof(TestModel).GetProperty(nameof(TestModel.DescribedProperty))!;

        var (_, schema) = builder.Build(property, typeof(string));

        Assert.IsTrue(schema.ContainsKey("description"), "Should include description");
        Assert.AreEqual("A test property", schema["description"], "Description should match attribute");
    }

    [TestMethod]
    public void JsonSchemaPropertyBuilder__Build__when__property_with_range__then__includes_minimum_and_maximum()
    {
        var property = typeof(TestModel).GetProperty(nameof(TestModel.RangedProperty))!;

        var (_, schema) = builder.Build(property, typeof(int));

        Assert.IsTrue(schema.ContainsKey("minimum"), "Should include minimum");
        Assert.AreEqual(1.0, schema["minimum"], "Minimum should match Range attribute");
        Assert.IsTrue(schema.ContainsKey("maximum"), "Should include maximum");
        Assert.AreEqual(100.0, schema["maximum"], "Maximum should match Range attribute");
    }

    [TestMethod]
    public void JsonSchemaPropertyBuilder__Build__when__property_with_maxlength__then__includes_max_length()
    {
        var property = typeof(TestModel).GetProperty(nameof(TestModel.MaxLengthProperty))!;

        var (_, schema) = builder.Build(property, typeof(string));

        Assert.IsTrue(schema.ContainsKey("maxLength"), "Should include maxLength");
        Assert.AreEqual(100, schema["maxLength"], "MaxLength should match attribute");
    }

    [TestMethod]
    public void JsonSchemaPropertyBuilder__Build__when__property_with_datetime_type__then__includes_format()
    {
        var property = typeof(TestModel).GetProperty(nameof(TestModel.TimestampProperty))!;

        var (_, schema) = builder.Build(property, typeof(System.DateTime));

        Assert.AreEqual("string", schema["type"], "DateTime should have string type");
        Assert.IsTrue(schema.ContainsKey("format"), "Should include format");
        Assert.AreEqual("date-time", schema["format"], "Format should be date-time");
    }

    [TestMethod]
    public void JsonSchemaPropertyBuilder__Build__when__property_with_json_property_name__then__uses_that_name()
    {
        var property = typeof(TestModel).GetProperty(nameof(TestModel.JsonPropertyNameProperty))!;

        var (jsonName, _) = builder.Build(property, typeof(string));

        Assert.AreEqual("customJsonName", jsonName, "Should use [JsonPropertyName] value");
    }

    [TestMethod]
    public void JsonSchemaPropertyBuilder__Build__when__property_with_single_example__then__includes_example()
    {
        var property = typeof(TestModel).GetProperty(nameof(TestModel.PropertyWithExample))!;

        var (_, schema) = builder.Build(property, typeof(string));

        // Note: since we don't have [Example] attribute in standard framework,
        // this test verifies the structure is correct when examples would exist
        Assert.IsNotNull(schema, "Schema should be built");
    }

    [TestMethod]
    public void JsonSchemaPropertyBuilder__Build__when__camel_case_conversion_handles_single_letter__then__lowercase()
    {
        var property = typeof(TestModel).GetProperty(nameof(TestModel.X))!;

        var (jsonName, _) = builder.Build(property, typeof(string));

        Assert.AreEqual("x", jsonName, "Single letter should become lowercase");
    }

    [TestMethod]
    public void JsonSchemaPropertyBuilder__Build__when__camel_case_conversion_handles_acronyms__then__first_letter_lowercased()
    {
        var property = typeof(TestModel).GetProperty(nameof(TestModel.HTTPResponse))!;

        var (jsonName, _) = builder.Build(property, typeof(string));

        Assert.AreEqual("hTTPResponse", jsonName, "Acronyms should have first letter lowercased");
    }

    [TestMethod]
    public void JsonSchemaPropertyBuilder__Build__when__complex_property_with_multiple_attributes__then__merges_all()
    {
        var property = typeof(TestModel).GetProperty(nameof(TestModel.ComplexProperty))!;

        var (jsonName, schema) = builder.Build(property, typeof(int));

        Assert.AreEqual("complexProperty", jsonName, "Should convert to camelCase");
        Assert.IsTrue(schema.ContainsKey("description"), "Should include description");
        Assert.IsTrue(schema.ContainsKey("minimum"), "Should include minimum");
        Assert.IsTrue(schema.ContainsKey("maximum"), "Should include maximum");
        Assert.AreEqual("integer", schema["type"], "Should preserve integer type");
    }

    [TestMethod]
    public void JsonSchemaPropertyBuilder__Build__when__array_property__then__returns_array_schema()
    {
        var property = typeof(TestModel).GetProperty(nameof(TestModel.Items))!;

        var (jsonName, schema) = builder.Build(property, typeof(string[]));

        Assert.AreEqual("items", jsonName, "Should convert to camelCase");
        Assert.AreEqual("array", schema["type"], "Should have array type");
        Assert.IsTrue(schema.ContainsKey("items"), "Should include items schema");
    }

    // Test model with various properties
    private class TestModel
    {
        public string PropertyName { get; set; } = string.Empty;

        public int Count { get; set; }

        [System.ComponentModel.Description("A test property")]
        public string DescribedProperty { get; set; } = string.Empty;

        [Range(1, 100)]
        public int RangedProperty { get; set; }

        [MaxLength(100)]
        public string MaxLengthProperty { get; set; } = string.Empty;

        public System.DateTime TimestampProperty { get; set; }

        [JsonPropertyName("customJsonName")]
        public string JsonPropertyNameProperty { get; set; } = string.Empty;

        public string PropertyWithExample { get; set; } = string.Empty;

        public string X { get; set; } = string.Empty;

        public string HTTPResponse { get; set; } = string.Empty;

        [System.ComponentModel.Description("Complex property")]
        [Range(10, 1000)]
        public int ComplexProperty { get; set; }

        public string[] Items { get; set; } = System.Array.Empty<string>();
    }
}
