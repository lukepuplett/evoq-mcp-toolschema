#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Evoq.Mcp.ToolSchema;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Evoq.Mcp.ToolSchema.Tests;

[TestClass]
public class PropertyAttributeReaderTests
{
    private readonly PropertyAttributeReader reader = new();

    [TestMethod]
    public void PropertyAttributeReader__Read__when__property_with_description__then__returns_description()
    {
        var property = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.DescribedProperty))!;

        var result = reader.Read(property);

        Assert.AreEqual("This is a test property", result.Description, "Should read Description attribute");
    }

    [TestMethod]
    public void PropertyAttributeReader__Read__when__property_without_description__then__returns_null_description()
    {
        var property = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.SimpleProperty))!;

        var result = reader.Read(property);

        Assert.IsNull(result.Description, "Should return null for description if not present");
    }

    [TestMethod]
    public void PropertyAttributeReader__Read__when__property_with_required__then__returns_is_required_true()
    {
        var property = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.RequiredProperty))!;

        var result = reader.Read(property);

        Assert.IsTrue(result.IsRequired, "Should detect [Required] attribute");
    }

    [TestMethod]
    public void PropertyAttributeReader__Read__when__property_without_required__then__returns_is_required_false()
    {
        var property = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.SimpleProperty))!;

        var result = reader.Read(property);

        Assert.IsFalse(result.IsRequired, "Should return false for IsRequired if not present");
    }

    [TestMethod]
    public void PropertyAttributeReader__Read__when__property_with_range__then__returns_minimum_and_maximum()
    {
        var property = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.RangedProperty))!;

        var result = reader.Read(property);

        Assert.AreEqual(1.0, result.Minimum, "Should extract minimum from Range attribute");
        Assert.AreEqual(100.0, result.Maximum, "Should extract maximum from Range attribute");
    }

    [TestMethod]
    public void PropertyAttributeReader__Read__when__property_without_range__then__returns_null_minimum_maximum()
    {
        var property = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.SimpleProperty))!;

        var result = reader.Read(property);

        Assert.IsNull(result.Minimum, "Should return null for Minimum if not present");
        Assert.IsNull(result.Maximum, "Should return null for Maximum if not present");
    }

    [TestMethod]
    public void PropertyAttributeReader__Read__when__property_with_maxlength__then__returns_max_length()
    {
        var property = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.MaxLengthProperty))!;

        var result = reader.Read(property);

        Assert.AreEqual(50, result.MaxLength, "Should extract MaxLength from attribute");
    }

    [TestMethod]
    public void PropertyAttributeReader__Read__when__property_without_maxlength__then__returns_null_max_length()
    {
        var property = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.SimpleProperty))!;

        var result = reader.Read(property);

        Assert.IsNull(result.MaxLength, "Should return null for MaxLength if not present");
    }

    [TestMethod]
    public void PropertyAttributeReader__Read__when__property_with_regex__then__returns_pattern()
    {
        var property = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.RegexProperty))!;

        var result = reader.Read(property);

        Assert.AreEqual(@"^\d{3}-\d{2}-\d{4}$", result.Pattern, "Should extract pattern from RegularExpression attribute");
    }

    [TestMethod]
    public void PropertyAttributeReader__Read__when__property_without_regex__then__returns_null_pattern()
    {
        var property = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.SimpleProperty))!;

        var result = reader.Read(property);

        Assert.IsNull(result.Pattern, "Should return null for Pattern if not present");
    }

    [TestMethod]
    public void PropertyAttributeReader__Read__when__property_with_multiple_attributes__then__combines_all()
    {
        var property = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.MultiAttributeProperty))!;

        var result = reader.Read(property);

        Assert.AreEqual("A complex property", result.Description, "Should have description");
        Assert.IsTrue(result.IsRequired, "Should be required");
        Assert.AreEqual(10.0, result.Minimum, "Should have minimum");
        Assert.AreEqual(500.0, result.Maximum, "Should have maximum");
    }

    // Test class with various attributes for testing
    private class TestClassWithAttributes
    {
        public string SimpleProperty { get; set; } = string.Empty;

        [System.ComponentModel.Description("This is a test property")]
        public string DescribedProperty { get; set; } = string.Empty;

        [Required]
        public string RequiredProperty { get; set; } = string.Empty;

        [Range(1, 100)]
        public int RangedProperty { get; set; }

        [MaxLength(50)]
        public string MaxLengthProperty { get; set; } = string.Empty;

        [RegularExpression(@"^\d{3}-\d{2}-\d{4}$")]
        public string RegexProperty { get; set; } = string.Empty;

        [System.ComponentModel.Description("A complex property")]
        [Required]
        [Range(10, 500)]
        public int MultiAttributeProperty { get; set; }
    }
}
