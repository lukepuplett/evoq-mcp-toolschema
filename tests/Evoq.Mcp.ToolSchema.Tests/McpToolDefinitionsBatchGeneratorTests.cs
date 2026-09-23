#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Evoq.Mcp.ToolSchema;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Evoq.Mcp.ToolSchema.Tests;

[TestClass]
public class McpToolDefinitionsBatchGeneratorTests
{
    private readonly McpToolDefinitionsBatchGenerator generator = new();

    [TestMethod]
    public void McpToolDefinitionsBatchGenerator__GenerateForController__when__single_controller__then__returns_all_qualifying_tools()
    {
        var results = generator.GenerateForController(typeof(BatchTestController));

        Assert.IsNotNull(results, "Should return results");
        Assert.IsTrue(results.Count > 0, "Should generate at least one tool definition");
    }

    [TestMethod]
    public void McpToolDefinitionsBatchGenerator__GenerateForController__when__multiple_actions__then__generates_for_each()
    {
        var results = generator.GenerateForController(typeof(BatchTestController));

        var toolCount = results.Count;
        Assert.IsTrue(toolCount >= 2, "Should generate multiple tool definitions for multiple actions");
    }

    [TestMethod]
    public void McpToolDefinitionsBatchGenerator__GenerateForController__when__each_result_has_required_fields__then__validation()
    {
        var results = generator.GenerateForController(typeof(BatchTestController));

        foreach (var tool in results)
        {
            Assert.IsTrue(tool.ContainsKey("name"), "Each tool should have name");
            Assert.IsTrue(tool.ContainsKey("description"), "Each tool should have description");
            Assert.IsTrue(tool.ContainsKey("inputSchema"), "Each tool should have inputSchema");
        }
    }

    [TestMethod]
    public void McpToolDefinitionsBatchGenerator__GenerateForControllers__when__multiple_controllers__then__returns_all_tools()
    {
        var controllerTypes = new[]
        {
            typeof(BatchTestController),
            typeof(BatchTest2Controller)
        };

        var results = generator.GenerateForControllers(controllerTypes);

        Assert.IsTrue(results.Count >= 2, "Should combine tools from multiple controllers");
    }

    [TestMethod]
    public void McpToolDefinitionsBatchGenerator__GenerateForAssembly__when__finds_and_generates_for_all_controllers__then__returns_results()
    {
        var assembly = typeof(BatchTestController).Assembly;

        var results = generator.GenerateForAssembly(assembly);

        // Should find the test controllers in this assembly
        Assert.IsTrue(results.Count > 0, "Should find and generate tools for controllers in assembly");
    }

    [TestMethod]
    public void McpToolDefinitionsBatchGenerator__GenerateForAssembly__when__result_is_serializable__then__valid()
    {
        var assembly = typeof(BatchTestController).Assembly;
        var results = generator.GenerateForAssembly(assembly);

        // Verify all results are IReadOnlyDictionary suitable for JSON serialization
        foreach (var tool in results)
        {
            Assert.IsNotNull(tool["name"], "name should not be null");
            Assert.IsNotNull(tool["description"], "description should not be null");
            Assert.IsNotNull(tool["inputSchema"], "inputSchema should not be null");
        }
    }

    // Test controllers
    [ApiController]
    [Route("api/batchtest")]
    private class BatchTestController : ControllerBase
    {
        [HttpGet]
        [System.ComponentModel.Description("Get items")]
        public IActionResult GetItems()
        {
            return Ok();
        }

        [HttpPost]
        [System.ComponentModel.Description("Create item")]
        public IActionResult Create([FromBody] ItemRequest request)
        {
            return Created("", request);
        }

        [HttpGet("skip-me")]
        public IActionResult NoDescription()
        {
            return Ok();
        }
    }

    [ApiController]
    [Route("api/batchtest2")]
    private class BatchTest2Controller : ControllerBase
    {
        [HttpGet]
        [System.ComponentModel.Description("Get settings")]
        public IActionResult GetSettings()
        {
            return Ok();
        }
    }

    private record ItemRequest(string Name);
}
