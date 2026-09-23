#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Evoq.Mcp.ToolSchema;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

namespace Evoq.Mcp.ToolSchema.Tests;

/// <summary>
/// Golden-output tests: a stub controller, action, and annotated request model produce
/// one exact, known MCP tool definition JSON. Compares parsed JSON trees (not raw strings)
/// so the assertion is immune to dictionary enumeration order.
/// </summary>
[TestClass]
[TestCategory("AllInMemory")]
[TestCategory("AllFast")]
public class GoldenJsonOutputTests
{
    private readonly McpToolDefinitionBuilder builder = new();

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__simple_get_action__then__produces_known_json()
    {
        var method = typeof(GoldenApiController).GetMethod(nameof(GoldenApiController.GetWidget))!;

        var result = builder.Build(method);

        AssertJsonEquals(
            """
            {
              "name": "golden_get_widget",
              "description": "Retrieves a widget by convention, no parameters",
              "inputSchema": {
                "$schema": "https://json-schema.org/draft/2020-12/schema",
                "type": "object",
                "properties": {},
                "additionalProperties": false
              },
              "annotations": {
                "readOnlyHint": true,
                "destructiveHint": false,
                "openWorldHint": false,
                "idempotentHint": false
              }
            }
            """,
            result);
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__post_with_required_and_ranged_body__then__produces_known_json()
    {
        var method = typeof(GoldenApiController).GetMethod(nameof(GoldenApiController.CreateWidget))!;

        var result = builder.Build(method);

        AssertJsonEquals(
            """
            {
              "name": "golden_create_widget",
              "description": "Creates a widget with a required name and a bounded priority",
              "inputSchema": {
                "$schema": "https://json-schema.org/draft/2020-12/schema",
                "type": "object",
                "properties": {
                  "name": {
                    "type": "string",
                    "description": "Widget display name"
                  },
                  "priority": {
                    "type": "integer",
                    "minimum": 1,
                    "maximum": 5
                  }
                },
                "additionalProperties": false,
                "required": ["name"]
              },
              "annotations": {
                "readOnlyHint": false,
                "destructiveHint": false,
                "openWorldHint": false,
                "idempotentHint": false
              }
            }
            """,
            result);
    }

    [TestMethod]
    public void McpToolDefinitionBuilder__Build__when__post_with_exactly_one_of_body__then__produces_known_json()
    {
        var method = typeof(GoldenApiController).GetMethod(nameof(GoldenApiController.LinkWidget))!;

        var result = builder.Build(method);

        AssertJsonEquals(
            """
            {
              "name": "golden_link_widget",
              "description": "Links a widget to a target identified by exactly one of two locators",
              "inputSchema": {
                "$schema": "https://json-schema.org/draft/2020-12/schema",
                "type": "object",
                "properties": {
                  "targetKey": { "type": "string" },
                  "targetName": { "type": "string" }
                },
                "additionalProperties": false,
                "allOf": [
                  {
                    "oneOf": [
                      { "required": ["targetKey"] },
                      { "required": ["targetName"] }
                    ]
                  }
                ]
              },
              "annotations": {
                "readOnlyHint": false,
                "destructiveHint": true,
                "openWorldHint": false,
                "idempotentHint": false
              }
            }
            """,
            result);
    }

    private static void AssertJsonEquals(string expectedJson, object actual)
    {
        var expectedNode = JsonNode.Parse(expectedJson);
        var actualNode = JsonNode.Parse(JsonSerializer.Serialize(actual));

        var equal = JsonNode.DeepEquals(expectedNode, actualNode);

        Assert.IsTrue(
            equal,
            $"Generated tool definition JSON did not match the expected golden JSON.\n" +
            $"Expected:\n{expectedNode?.ToJsonString(new JsonSerializerOptions { WriteIndented = true })}\n\n" +
            $"Actual:\n{actualNode?.ToJsonString(new JsonSerializerOptions { WriteIndented = true })}");
    }

    // Stub controller + models for the golden JSON assertions above.
    [ApiController]
    [Route("api/golden")]
    private class GoldenApiController : ControllerBase
    {
        [HttpGet]
        [System.ComponentModel.Description("Retrieves a widget by convention, no parameters")]
        public IActionResult GetWidget()
        {
            return Ok();
        }

        [HttpPost]
        [System.ComponentModel.Description("Creates a widget with a required name and a bounded priority")]
        public IActionResult CreateWidget([FromBody] CreateWidgetRequest request)
        {
            return Created("", request);
        }

        [HttpPost("link")]
        [ToolHints(destructive: true)]
        [System.ComponentModel.Description("Links a widget to a target identified by exactly one of two locators")]
        public IActionResult LinkWidget([FromBody] LinkWidgetRequest request)
        {
            return Ok();
        }
    }

    private class CreateWidgetRequest
    {
        [Required]
        [System.ComponentModel.Description("Widget display name")]
        public string Name { get; set; } = string.Empty;

        [Range(1, 5)]
        public int? Priority { get; set; }
    }

    [ExactlyOneOf(nameof(TargetKey), nameof(TargetName))]
    private class LinkWidgetRequest
    {
        public string? TargetKey { get; set; }
        public string? TargetName { get; set; }
    }
}
