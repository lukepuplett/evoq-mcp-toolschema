#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Evoq.Mcp.ToolSchema;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Evoq.Mcp.ToolSchema.Tests;

[TestClass]
[TestCategory("AllInMemory")]
[TestCategory("AllFast")]
public class McpToolDefinitionBuilderTests
{
    private readonly McpToolDefinitionBuilder builder = new();

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__simple_get_action__then__returns_complete_tool_definition()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.GetItems))!;

        var result = builder.Build(method);

        Assert.IsNotNull(result, "Should return tool definition");
        Assert.AreEqual("stub_get_items", result["name"], "Should have correct tool name");
        Assert.AreEqual("Retrieves items", result["description"], "Should have description");
        Assert.IsTrue(result.ContainsKey("inputSchema"), "Should have inputSchema");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__action_with_query_parameters__then__includes_in_schema()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.GetFiltered))!;

        var result = builder.Build(method);

        var inputSchema = result["inputSchema"] as IDictionary<string, object?>;
        Assert.IsNotNull(inputSchema, "Should have inputSchema");
        Assert.AreEqual("object", inputSchema["type"], "Schema should be object type");
        Assert.IsTrue(inputSchema.ContainsKey("properties"), "Should have properties");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__action_with_body_parameter__then__includes_in_schema()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.Create))!;

        var result = builder.Build(method);

        var inputSchema = result["inputSchema"] as IDictionary<string, object?>;
        Assert.IsNotNull(inputSchema, "Should have inputSchema");
        Assert.IsTrue(inputSchema.ContainsKey("properties"), "Should have properties for body");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__multiple_parameters__then__merges_all()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.Update))!;

        var result = builder.Build(method);

        var inputSchema = result["inputSchema"] as IDictionary<string, object?>;
        var properties = inputSchema?["properties"] as IDictionary<string, object?>;

        // Should include both route parameter and body
        Assert.IsTrue(properties?.Count > 0, "Should have merged properties from multiple params");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__tool_name__then__strips_controller_and_api_suffixes()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.GetItems))!;

        var result = builder.Build(method);

        var toolName = result["name"] as string;
        Assert.IsFalse(toolName?.Contains("Controller") ?? false, "Tool name should not contain 'Controller'");
        Assert.IsFalse(toolName?.Contains("Api") ?? false, "Tool name should not contain 'Api'");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__extracts_description_from_attribute__then__has_description()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.GetItems))!;

        var result = builder.Build(method);

        var description = result["description"] as string;
        Assert.IsNotNull(description, "Should have description");
        Assert.IsTrue(description!.Length > 0, "Description should not be empty");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__two_string_route_parameters__then__schema_has_named_string_properties()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.GetAttachmentByRoute))!;

        var result = builder.Build(method);

        var inputSchema = result["inputSchema"] as IDictionary<string, object?>;
        var properties = inputSchema?["properties"] as IDictionary<string, object?>;
        Assert.IsNotNull(properties, "Should have properties");
        Assert.IsTrue(properties!.ContainsKey("timesheetKey"), "Should expose timesheetKey");
        Assert.IsTrue(properties.ContainsKey("fileName"), "Should expose fileName");
        Assert.IsFalse(properties.ContainsKey("chars"), "Must not reflect System.String Chars");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__body_has_required_and_optional_properties__then__only_required_marked_in_schema()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.CreateWithMixedRequired))!;

        var result = builder.Build(method);

        var inputSchema = result["inputSchema"] as IDictionary<string, object?>;
        Assert.IsNotNull(inputSchema, "Should have inputSchema");

        var requiredArray = inputSchema!["required"] as IReadOnlyList<string>;
        Assert.IsNotNull(requiredArray, "Should have required array");

        var requiredList = string.Join(", ", requiredArray!);
        System.Diagnostics.Debug.WriteLine($"Required properties: [{requiredList}]");

        // Should contain only properties marked with [Required]
        Assert.IsTrue(requiredArray!.Contains("requiredName"), "Should include [Required] property 'requiredName'");
        Assert.IsTrue(requiredArray!.Contains("requiredId"), "Should include [Required] property 'requiredId'");

        // Should NOT contain optional properties (nullable or not)
        Assert.IsFalse(requiredArray!.Contains("optionalDescription"), "Should NOT include optional nullable property");
        Assert.IsFalse(requiredArray!.Contains("optionalNotes"), "Should NOT include optional nullable property");

        Assert.AreEqual(2, requiredArray!.Count, $"Should have exactly 2 required properties, but got: [{requiredList}]");
    }

    // -------------------------------------------------------------------------
    // annotations block tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__http_get__then__annotations_readOnly_true_others_false()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.GetItems))!;

        var result = builder.Build(method);

        var annotations = result["annotations"] as IDictionary<string, object?>;
        Assert.IsNotNull(annotations, "Should include annotations block");
        Assert.AreEqual(true,  annotations!["readOnlyHint"],   "GET should derive readOnlyHint=true");
        Assert.AreEqual(false, annotations!["destructiveHint"], "GET should derive destructiveHint=false");
        Assert.AreEqual(false, annotations!["openWorldHint"],  "GET should derive openWorldHint=false");
        Assert.AreEqual(false, annotations!["idempotentHint"], "GET should derive idempotentHint=false");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__http_post_no_hints__then__annotations_all_false()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.Create))!;

        var result = builder.Build(method);

        var annotations = result["annotations"] as IDictionary<string, object?>;
        Assert.IsNotNull(annotations, "Should include annotations block");
        Assert.AreEqual(false, annotations!["readOnlyHint"],   "POST without hints should have readOnlyHint=false");
        Assert.AreEqual(false, annotations!["destructiveHint"], "POST without hints should have destructiveHint=false");
        Assert.AreEqual(false, annotations!["openWorldHint"],  "POST without hints should have openWorldHint=false");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__tool_hints_destructive__then__annotations_destructive_true()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.DeleteItem))!;

        var result = builder.Build(method);

        var annotations = result["annotations"] as IDictionary<string, object?>;
        Assert.IsNotNull(annotations, "Should include annotations block");
        Assert.AreEqual(false, annotations!["readOnlyHint"],   "Destructive POST should have readOnlyHint=false");
        Assert.AreEqual(true,  annotations!["destructiveHint"], "[ToolHints(destructive:true)] should set destructiveHint=true");
        Assert.AreEqual(false, annotations!["openWorldHint"],  "Destructive POST should have openWorldHint=false");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__tool_hints_open_world__then__annotations_openWorld_true()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.SendNotification))!;

        var result = builder.Build(method);

        var annotations = result["annotations"] as IDictionary<string, object?>;
        Assert.IsNotNull(annotations, "Should include annotations block");
        Assert.AreEqual(false, annotations!["readOnlyHint"],   "Open-world POST should have readOnlyHint=false");
        Assert.AreEqual(false, annotations!["destructiveHint"], "Open-world POST should have destructiveHint=false");
        Assert.AreEqual(true,  annotations!["openWorldHint"],  "[ToolHints(openWorld:true)] should set openWorldHint=true");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__get_with_open_world_override__then__annotations_readOnly_and_openWorld_true()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.GetFromExternalApi))!;

        var result = builder.Build(method);

        var annotations = result["annotations"] as IDictionary<string, object?>;
        Assert.IsNotNull(annotations, "Should include annotations block");
        Assert.AreEqual(true, annotations!["readOnlyHint"],  "GET convention still applies: readOnlyHint=true");
        Assert.AreEqual(true, annotations!["openWorldHint"], "[ToolHints(openWorld:true)] override adds openWorldHint=true");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__post_destructive_and_open_world__then__both_flags_true()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.RetractAndNotify))!;

        var result = builder.Build(method);

        var annotations = result["annotations"] as IDictionary<string, object?>;
        Assert.IsNotNull(annotations, "Should include annotations block");
        Assert.AreEqual(true, annotations!["destructiveHint"], "Should have destructiveHint=true");
        Assert.AreEqual(true, annotations!["openWorldHint"],  "Should have openWorldHint=true");
        Assert.AreEqual(false, annotations!["readOnlyHint"],  "Should have readOnlyHint=false");
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__ToolName_attribute_present__then__uses_override_not_convention()
    {
        var method = typeof(StubApiController).GetMethod(nameof(StubApiController.PostFeedbackLegacy))!;

        var result = builder.Build(method);

        Assert.AreEqual("stub_submit_feedback", result["name"], "Should use [ToolName] value, not convention-derived name");
    }

    // Stub controller for testing
    [ApiController]
    [Route("api/stub")]
    private class StubApiController : ControllerBase
    {
        [HttpGet]
        [System.ComponentModel.Description("Retrieves items")]
        public IActionResult GetItems()
        {
            return Ok();
        }

        [HttpGet("filtered")]
        [System.ComponentModel.Description("Get filtered items")]
        public IActionResult GetFiltered(string? filter, int? limit)
        {
            return Ok();
        }

        [HttpPost]
        [System.ComponentModel.Description("Creates a new item")]
        public IActionResult Create([FromBody] CreateItemRequest request)
        {
            return Created("", request);
        }

        [HttpPut("{id}")]
        [System.ComponentModel.Description("Updates an item")]
        public IActionResult Update(string id, [FromBody] UpdateItemRequest request)
        {
            return Ok();
        }

        [HttpGet("{timesheetKey}/attachments/{fileName}")]
        [System.ComponentModel.Description("Downloads attachment")]
        public IActionResult GetAttachmentByRoute([FromRoute] string timesheetKey, [FromRoute] string fileName)
        {
            return Ok();
        }

        [HttpPost("with-mixed-required")]
        [System.ComponentModel.Description("Creates item with mixed required/optional properties")]
        public IActionResult CreateWithMixedRequired([FromBody] MixedRequiredRequest request)
        {
            return Created("", request);
        }

        [HttpPost("delete/{id}")]
        [ToolHints(destructive: true)]
        [System.ComponentModel.Description("Deletes an item permanently")]
        public IActionResult DeleteItem(string id)
        {
            return Ok();
        }

        [HttpPost("notify")]
        [ToolHints(openWorld: true)]
        [System.ComponentModel.Description("Sends a notification to an external system")]
        public IActionResult SendNotification([FromBody] CreateItemRequest request)
        {
            return Ok();
        }

        [HttpGet("external")]
        [ToolHints(openWorld: true)]
        [System.ComponentModel.Description("Fetches data from an external OAuth provider")]
        public IActionResult GetFromExternalApi()
        {
            return Ok();
        }

        [HttpPost("retract/{id}")]
        [ToolHints(destructive: true, openWorld: true)]
        [System.ComponentModel.Description("Retracts a submission and notifies external recipients")]
        public IActionResult RetractAndNotify(string id)
        {
            return Ok();
        }

        [HttpPost("feedback")]
        [ToolName("stub_submit_feedback")]
        [System.ComponentModel.Description("Submits feedback")]
        public IActionResult PostFeedbackLegacy()
        {
            return Ok();
        }
    }

    private record CreateItemRequest(string Name, string? Description = null);
    private record UpdateItemRequest(string Name, string? Description = null);

    private record MixedRequiredRequest(
        [property: System.ComponentModel.DataAnnotations.Required] string RequiredName,
        [property: System.ComponentModel.DataAnnotations.Required] int RequiredId,
        string? OptionalDescription = null,
        string? OptionalNotes = null);
}
