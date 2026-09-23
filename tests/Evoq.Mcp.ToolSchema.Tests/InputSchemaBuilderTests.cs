#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Evoq.Mcp.ToolSchema;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Evoq.Mcp.ToolSchema.Tests;

[TestClass]
public class InputSchemaBuilderTests
{
    private readonly InputSchemaBuilder builder = new();

    private static (Type Type, bool IsBody, string? ParameterName) Param(Type type, bool isBody, string? name = null)
        => (type, isBody, name);

    [TestMethod]
    public void InputSchemaBuilder__Build__when__body_param_is_bare_string__then__throws_UnsupportedBodyParameterTypeException()
    {
        var ex = Assert.ThrowsException<UnsupportedBodyParameterTypeException>(
            () => builder.Build(new[] { Param(typeof(string), isBody: true) }),
            "A bare string [FromBody] parameter must be rejected, not reflected for its Length/Chars members");

        Assert.AreEqual(typeof(string), ex.ParameterType, "Exception should carry the offending parameter type");
        StringAssert.Contains(ex.Message, "request DTO", "Message should explain the fix");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__body_param_is_bare_int__then__throws_UnsupportedBodyParameterTypeException()
    {
        Assert.ThrowsException<UnsupportedBodyParameterTypeException>(
            () => builder.Build(new[] { Param(typeof(int), isBody: true) }),
            "A bare int [FromBody] parameter must be rejected");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__body_param_is_bare_string_list__then__throws_UnsupportedBodyParameterTypeException()
    {
        Assert.ThrowsException<UnsupportedBodyParameterTypeException>(
            () => builder.Build(new[] { Param(typeof(List<string>), isBody: true) }),
            "A bare List<string> [FromBody] parameter must be rejected, not reflected for its Capacity/Count members");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__route_param_is_bare_string__then__does_not_throw()
    {
        var schema = builder.Build(new[] { Param(typeof(string), isBody: false, name: "fileName") });

        var properties = schema["properties"] as IDictionary<string, object?>;
        Assert.IsTrue(properties?.ContainsKey("fileName"), "Route/query primitives are fine and become a named property");
    }

    private enum WidgetStatus { Draft, Published, Archived }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__non_body_param_is_bare_Guid__then__named_property_with_uuid_format()
    {
        var schema = builder.Build(new[] { Param(typeof(Guid), isBody: false, name: "widgetId") });

        var properties = schema["properties"] as IDictionary<string, object?>;
        var widgetId = properties?["widgetId"] as IDictionary<string, object?>;
        Assert.IsNotNull(widgetId, "Guid route/query param should become a named property");
        Assert.AreEqual("string", widgetId!["type"], "Guid should map to schema type string");
        Assert.AreEqual("uuid", widgetId["format"], "Guid should carry format uuid");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__non_body_param_is_nullable_DateTime__then__named_property_with_date_time_format()
    {
        var schema = builder.Build(new[] { Param(typeof(DateTime?), isBody: false, name: "since") });

        var properties = schema["properties"] as IDictionary<string, object?>;
        var since = properties?["since"] as IDictionary<string, object?>;
        Assert.IsNotNull(since, "Nullable DateTime route/query param should become a named property");
        Assert.AreEqual("string", since!["type"], "DateTime should map to schema type string");
        Assert.AreEqual("date-time", since["format"], "DateTime should carry format date-time");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__non_body_param_is_bare_bool__then__named_property_with_boolean_type()
    {
        var schema = builder.Build(new[] { Param(typeof(bool), isBody: false, name: "includeArchived") });

        var properties = schema["properties"] as IDictionary<string, object?>;
        var includeArchived = properties?["includeArchived"] as IDictionary<string, object?>;
        Assert.IsNotNull(includeArchived, "bool route/query param should become a named property");
        Assert.AreEqual("boolean", includeArchived!["type"], "bool should map to schema type boolean");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__non_body_param_is_bare_enum__then__named_property_with_enum_values()
    {
        var schema = builder.Build(new[] { Param(typeof(WidgetStatus), isBody: false, name: "status") });

        var properties = schema["properties"] as IDictionary<string, object?>;
        var status = properties?["status"] as IDictionary<string, object?>;
        Assert.IsNotNull(status, "Enum route/query param should become a named property");
        Assert.AreEqual("string", status!["type"], "Enum should map to schema type string");
        var enumValues = status["enum"] as IEnumerable<string>;
        CollectionAssert.AreEqual(
            new[] { "Draft", "Published", "Archived" },
            enumValues?.ToList(),
            "Enum property should list all member names");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__single_record_type__then__returns_object_with_all_properties()
    {
        var schema = builder.Build(new[] { Param(typeof(SimpleRecord), false) });

        Assert.AreEqual("object", schema["type"], "Should have type object");
        Assert.IsTrue(schema.ContainsKey("properties"), "Should have properties");

        var properties = schema["properties"] as IDictionary<string, object?>;
        Assert.IsNotNull(properties, "Properties should be a dictionary");
        Assert.IsTrue(properties?.ContainsKey("id"), "Should have id property");
        Assert.IsTrue(properties?.ContainsKey("name"), "Should have name property");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__record_type_with_required__then__marks_required_properties()
    {
        var schema = builder.Build(new[] { Param(typeof(RecordWithRequired), false) });

        Assert.IsTrue(schema.ContainsKey("required"), "Should have required array");
        var required = schema["required"] as IReadOnlyList<string>;
        Assert.IsNotNull(required, "Required should be list");
        Assert.IsTrue(required?.Contains("value"), "Required properties should be in array");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__two_types__then__merges_all_properties()
    {
        var schema = builder.Build(new[] { Param(typeof(Record1), false), Param(typeof(Record2), false) });

        var properties = schema["properties"] as IDictionary<string, object?>;
        Assert.IsNotNull(properties, "Should have properties");
        Assert.IsTrue(properties?.ContainsKey("prop1"), "Should have prop1 from Record1");
        Assert.IsTrue(properties?.ContainsKey("prop2"), "Should have prop2 from Record2");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void InputSchemaBuilder__Build__when__colliding_property_names__then__throws_exception()
    {
        var schema = builder.Build(new[] { Param(typeof(Record1), false), Param(typeof(CollisionRecord), false) });

        // Should throw because both have "prop1"
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__body_parameter_with_required__then__includes_in_required()
    {
        var schema = builder.Build(new[] { Param(typeof(RecordWithRequired), true) });

        // Body parameter with [Required] must produce a required array
        Assert.IsTrue(schema.ContainsKey("required"), "Body parameter with [Required] should produce required array");
        var required = schema["required"] as IReadOnlyList<string>;
        Assert.IsNotNull(required, "Required should be list");
        Assert.IsTrue(required?.Contains("value"), "Required body property should be in required");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__no_required_properties__then__omits_required_field()
    {
        var schema = builder.Build(new[] { Param(typeof(SimpleRecord), false) });

        Assert.IsFalse(schema.ContainsKey("required"), "Required array should be omitted when no properties are required");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__camel_case_properties__then__uses_camel_case_keys()
    {
        var schema = builder.Build(new[] { Param(typeof(PascalCaseRecord), false) });

        var properties = schema["properties"] as IDictionary<string, object?>;
        Assert.IsTrue(properties?.ContainsKey("firstName"), "Should use camelCase key for FirstName");
        Assert.IsTrue(properties?.ContainsKey("lastName"), "Should use camelCase key for LastName");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__query_route_params_with_non_nullable_value_type_no_required__then__not_marked_required()
    {
        var schema = builder.Build(new[] { Param(typeof(QueryWithValueType), false) });

        // Query/route param with non-nullable value type but no [Required] should not be required
        // (Task #28: query params required only if explicitly [Required], not just non-nullable)
        var hasRequired = schema.ContainsKey("required");
        if (hasRequired)
        {
            var required = schema["required"] as IReadOnlyList<string>;
            Assert.IsNotNull(required, "Should have required array");
            Assert.IsFalse(required?.Contains("count"), "Non-nullable value type without [Required] should not be in required array for query params");
        }
        // If no required array at all, that's also acceptable (and expected for query params with no [Required])
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__enum_property_with_description__then__includes_enum_values_and_description()
    {
        // Arrange
        var schema = builder.Build(new[] { Param(typeof(RecordWithEnumAndDescription), false) });
        var properties = schema["properties"] as IDictionary<string, object?>;
        var statusProp = properties?["status"] as IDictionary<string, object?>;

        // Assert: enum values are included
        Assert.IsNotNull(statusProp, "Should have status property schema");
        Assert.IsTrue(statusProp?.ContainsKey("enum"), "Status property should have enum values");

        var enumValues = statusProp?["enum"] as IEnumerable<string>;
        Assert.IsNotNull(enumValues, "Enum should be a collection of strings");
        Assert.IsTrue(enumValues?.Contains("Draft"), "Enum should contain Draft");
        Assert.IsTrue(enumValues?.Contains("Sent"), "Enum should contain Sent");
        Assert.IsTrue(enumValues?.Contains("Approved"), "Enum should contain Approved");

        // Assert: description is included
        Assert.IsTrue(statusProp?.ContainsKey("description"), "Status property should have description");
        var description = statusProp?["description"] as string;
        Assert.AreEqual("Filter by timesheet status. Valid values: Draft, Sent, Approved.", description,
            "Description should match attribute");

        // Assert: type is string
        Assert.AreEqual("string", statusProp?["type"], "Enum property should have type string");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__string_property_with_AllowedValues__then__includes_enum_values_and_description()
    {
        // Arrange - string parameter with [AllowedValues] (no CLR enum)
        var schema = builder.Build(new[] { Param(typeof(RecordWithAllowedValuesOnString), false) });
        var properties = schema["properties"] as IDictionary<string, object?>;
        var statusProp = properties?["status"] as IDictionary<string, object?>;

        // Assert: enum values are included (from AllowedValues attribute)
        Assert.IsNotNull(statusProp, "Should have status property schema");
        Assert.IsTrue(statusProp?.ContainsKey("enum"), "String property with [AllowedValues] should have enum values");

        var enumValues = statusProp?["enum"] as IEnumerable<string>;
        Assert.IsNotNull(enumValues, "Enum should be a collection of strings");
        var enumList = enumValues?.ToList();
        Assert.AreEqual(3, enumList?.Count, "Should have exactly 3 enum values");
        Assert.IsTrue(enumList?.Contains("Draft"), "Enum should contain Draft");
        Assert.IsTrue(enumList?.Contains("Sent"), "Enum should contain Sent");
        Assert.IsTrue(enumList?.Contains("Approved"), "Enum should contain Approved");

        // Assert: description is included
        Assert.IsTrue(statusProp?.ContainsKey("description"), "Status property should have description");
        var description = statusProp?["description"] as string;
        Assert.AreEqual("Filter by timesheet status.", description,
            "Description should match attribute");

        // Assert: type is string
        Assert.AreEqual("string", statusProp?["type"], "String property should have type string");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__parameter_contains_nested_object_array__then__includes_complete_nested_schema_with_all_properties()
    {
        // Arrange - a request DTO that has an array of nested WorkflowStep objects
        var schema = builder.Build(new[] { Param(typeof(CreateWorkflowRequest), false) });

        // Navigate to the nested object properties
        var properties = schema["properties"] as IDictionary<string, object?>;
        Assert.IsNotNull(properties, "Should have top-level properties");

        var stepsSchema = properties?["steps"] as IDictionary<string, object?>;
        Assert.IsNotNull(stepsSchema, "Should have steps array property");

        var stepItems = stepsSchema?["items"] as IDictionary<string, object?>;
        Assert.IsNotNull(stepItems, "Array items should have schema");

        var stepProperties = stepItems?["properties"] as IDictionary<string, object?>;
        Assert.IsNotNull(stepProperties, "Nested WorkflowStep should have properties documented");

        // Assert: all nested properties are present
        Assert.IsTrue(stepProperties?.ContainsKey("stepType"), "Should include stepType property");
        Assert.IsTrue(stepProperties?.ContainsKey("approverId"), "Should include approverId property");
        Assert.IsTrue(stepProperties?.ContainsKey("requiredApprovals"), "Should include requiredApprovals property");

        // Assert: descriptions propagate to nested properties
        var stepTypeSchema = stepProperties?["stepType"] as IDictionary<string, object?>;
        Assert.IsTrue(stepTypeSchema?.ContainsKey("description"), "stepType should have description");
        Assert.AreEqual("Type of step: Approve or Process", stepTypeSchema?["description"]);

        // Assert: enum constraints on nested string properties
        Assert.IsTrue(stepTypeSchema?.ContainsKey("enum"), "stepType should have enum constraint");
        var enumValues = stepTypeSchema?["enum"] as IEnumerable<string>;
        var enumList = enumValues?.ToList();
        Assert.AreEqual(2, enumList?.Count, "Should have 2 enum options");
        Assert.IsTrue(enumList?.Contains("Approve"), "Should include Approve");
        Assert.IsTrue(enumList?.Contains("Process"), "Should include Process");

        // Assert: required fields on nested object
        var requiredFields = stepItems?["required"] as IReadOnlyList<string>;
        Assert.IsNotNull(requiredFields, "Nested object should specify required fields");
        Assert.IsTrue(requiredFields?.Contains("stepType"), "stepType should be required");
        Assert.IsTrue(requiredFields?.Contains("approverId"), "approverId should be required");
        Assert.IsFalse(requiredFields?.Contains("requiredApprovals"), "requiredApprovals is optional");

        // Assert: nested property types are correct
        var approverIdSchema = stepProperties?["approverId"] as IDictionary<string, object?>;
        Assert.AreEqual("string", approverIdSchema?["type"], "approverId should be string");

        var approvalsSchema = stepProperties?["requiredApprovals"] as IDictionary<string, object?>;
        Assert.AreEqual("integer", approvalsSchema?["type"], "requiredApprovals should be integer");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__type_has_AtLeastOneOf_and_ExactlyOneOf__then__schema_has_allOf_with_anyOf_and_oneOf_and_constrained_properties_not_in_required()
    {
        // Arrange: request type with conditional constraints (e.g. rename-activities style)
        var schema = builder.Build(new[] { Param(typeof(RequestWithConditionalConstraints), true) });

        // Assert: all four properties are present (camelCase)
        var properties = schema["properties"] as IDictionary<string, object?>;
        Assert.IsNotNull(properties, "Should have properties");
        Assert.IsTrue(properties?.ContainsKey("fromKeys"), "Should have fromKeys");
        Assert.IsTrue(properties?.ContainsKey("fromDisplayNames"), "Should have fromDisplayNames");
        Assert.IsTrue(properties?.ContainsKey("toKey"), "Should have toKey");
        Assert.IsTrue(properties?.ContainsKey("toDisplayName"), "Should have toDisplayName");

        // Assert: schema has allOf with composition constraints (anyOf + oneOf)
        Assert.IsTrue(schema.ContainsKey("allOf"), "Schema should have allOf for conditional constraints");
        var allOf = schema["allOf"] as IList<object?>;
        Assert.IsNotNull(allOf, "allOf should be a list");
        Assert.AreEqual(2, allOf.Count, "allOf should contain two constraints: anyOf (source) and oneOf (target)");

        // Assert: one entry is anyOf "at least one of fromKeys, fromDisplayNames"
        var anyOfSchema = allOf!.Select(x => x as IDictionary<string, object?>).FirstOrDefault(d => d != null && d.ContainsKey("anyOf"));
        Assert.IsNotNull(anyOfSchema, "One allOf entry should be anyOf");
        var anyOf = anyOfSchema!["anyOf"] as IList<object?>;
        Assert.IsNotNull(anyOf, "anyOf should be a list");
        Assert.AreEqual(2, anyOf.Count, "anyOf should have two alternatives");
        var anyOfRequireds = anyOf!.Cast<IDictionary<string, object?>>()
            .Select(d => d.TryGetValue("required", out var r) ? r as IReadOnlyList<string> : null)
            .Where(r => r != null).ToList()!;
        Assert.IsTrue(anyOfRequireds.Any(r => r?.Contains("fromKeys") == true), "anyOf should include alternative requiring fromKeys");
        Assert.IsTrue(anyOfRequireds.Any(r => r?.Contains("fromDisplayNames") == true), "anyOf should include alternative requiring fromDisplayNames");

        // Assert: one entry is oneOf "exactly one of toKey, toDisplayName"
        var oneOfSchema = allOf.Select(x => x as IDictionary<string, object?>).FirstOrDefault(d => d != null && d.ContainsKey("oneOf"));
        Assert.IsNotNull(oneOfSchema, "One allOf entry should be oneOf");
        var oneOf = oneOfSchema!["oneOf"] as IList<object?>;
        Assert.IsNotNull(oneOf, "oneOf should be a list");
        Assert.AreEqual(2, oneOf.Count, "oneOf should have two alternatives");
        var oneOfRequireds = oneOf!.Cast<IDictionary<string, object?>>()
            .Select(d => d.TryGetValue("required", out var r) ? r as IReadOnlyList<string> : null)
            .Where(r => r != null).ToList()!;
        Assert.IsTrue(oneOfRequireds.Any(r => r?.Contains("toKey") == true), "oneOf should include alternative requiring toKey");
        Assert.IsTrue(oneOfRequireds.Any(r => r?.Contains("toDisplayName") == true), "oneOf should include alternative requiring toDisplayName");

        // Assert: constrained properties are not in top-level required (constraint is expressed via allOf)
        var hasRequired = schema.TryGetValue("required", out var requiredValue);
        if (hasRequired && requiredValue is IReadOnlyList<string> required)
        {
            Assert.IsFalse(required.Contains("fromKeys"), "fromKeys should not be in top-level required");
            Assert.IsFalse(required.Contains("fromDisplayNames"), "fromDisplayNames should not be in top-level required");
            Assert.IsFalse(required.Contains("toKey"), "toKey should not be in top-level required");
            Assert.IsFalse(required.Contains("toDisplayName"), "toDisplayName should not be in top-level required");
        }
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__two_string_route_parameters__then__uses_param_names_not_string_reflection()
    {
        var schema = builder.Build(new[]
        {
            Param(typeof(string), false, "timesheetKey"),
            Param(typeof(string), false, "fileName"),
        });

        var properties = schema["properties"] as IDictionary<string, object?>;
        Assert.IsNotNull(properties, "Should have properties");
        Assert.IsTrue(properties!.ContainsKey("timesheetKey"), "Should have timesheetKey from param name");
        Assert.IsTrue(properties.ContainsKey("fileName"), "Should have fileName from param name");
        Assert.IsFalse(properties.ContainsKey("chars"), "Must not reflect System.String Chars indexer");
        Assert.IsFalse(properties.ContainsKey("length"), "Must not reflect System.String Length property");
    }

    [TestMethod]
    public void InputSchemaBuilder__Build__when__route_and_body_share_property_name__then__route_wins_and_body_duplicate_skipped()
    {
        var schema = builder.Build(new[]
        {
            Param(typeof(RouteLocatorWithStartIsoDate), false, "locator"),
            Param(typeof(BodyWithOptionalStartIsoDate), true),
        });

        var properties = schema["properties"] as IDictionary<string, object?>;
        Assert.IsNotNull(properties, "Should have properties");
        Assert.IsTrue(properties!.ContainsKey("startIsoDate"), "Route startIsoDate should be in schema");
        Assert.IsTrue(properties.ContainsKey("pay"), "Body-only pay should be in schema");
        Assert.AreEqual(5, properties.Count, "Should have four route keys plus body pay; duplicate body startIsoDate skipped");
    }

    // Test records
    private sealed class RouteLocatorWithStartIsoDate
    {
        [System.Text.Json.Serialization.JsonPropertyName("workplaceKey")]
        public string WorkplaceKey { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("assignmentKey")]
        public string AssignmentKey { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("senderAccountKey")]
        public string SenderAccountKey { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("startIsoDate")]
        public string StartIsoDate { get; set; } = string.Empty;
    }

    private sealed class BodyWithOptionalStartIsoDate
    {
        [System.Text.Json.Serialization.JsonPropertyName("startIsoDate")]
        public string? StartIsoDate { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("pay")]
        public string? Pay { get; set; }
    }

    private record SimpleRecord(string? Id, string? Name);

    private record RecordWithRequired([Required] string Value, string? Optional);

    private record Record1(string? Prop1, string? Other);

    private record Record2(string? Prop2, string? Stuff);

    private record CollisionRecord(string? Prop1); // Collides with Record1

    private record PascalCaseRecord(string? FirstName, string? LastName);

    private record QueryWithValueType(int Count, string? Name); // Non-nullable value type, no [Required]

    private record RecordWithEnumAndDescription(
        [property: System.ComponentModel.Description("Filter by timesheet status. Valid values: Draft, Sent, Approved.")]
        TimesheetStatus Status);

    private record RecordWithAllowedValuesOnString(
        [property: System.ComponentModel.Description("Filter by timesheet status.")]
        [property: AllowedValues("Draft", "Sent", "Approved")]
        string? Status);

    private record CreateWorkflowRequest(
        [Required]
        [property: System.ComponentModel.Description("Array of workflow steps")]
        WorkflowStep[] Steps);

    private record WorkflowStep(
        [Required]
        [property: System.ComponentModel.Description("Type of step: Approve or Process")]
        [property: AllowedValues("Approve", "Process")]
        string StepType,

        [Required]
        [property: System.ComponentModel.Description("User ID of approver")]
        string ApproverId,

        [property: System.ComponentModel.Description("Number of approvals needed")]
        int RequiredApprovals);

    private enum TimesheetStatus
    {
        Draft,
        Sent,
        Approved
    }

    /// <summary>
    /// Test-only request type: at least one of FromKeys/FromDisplayNames; exactly one of ToKey/ToDisplayName.
    /// </summary>
    [AtLeastOneOf(nameof(FromKeys), nameof(FromDisplayNames))]
    [ExactlyOneOf(nameof(ToKey), nameof(ToDisplayName))]
    private class RequestWithConditionalConstraints
    {
        public List<string>? FromKeys { get; set; }
        public List<string>? FromDisplayNames { get; set; }
        public string? ToKey { get; set; }
        public string? ToDisplayName { get; set; }
    }
}
