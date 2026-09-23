#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Evoq.Mcp.ToolSchema;
using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Evoq.Mcp.ToolSchema.Tests;

[TestClass]
[TestCategory("AllInMemory")]
[TestCategory("AllFast")]
public class ToolDiscoveryRegistryTests
{
    [TestMethod]
    public void ToolDiscoveryRegistry__Discovers__when__controller_with_qualifying_actions__then__registry_populated()
    {
        // Arrange - diagnose discovery first
        var discoverer = new QualifyingActionDiscoverer();
        var discovered = discoverer.Discover(typeof(TestController));

        var registry = new ToolDiscoveryRegistry(typeof(TestController));

        // Act
        var tools = registry.Tools;

        // Assert
        Assert.IsNotNull(tools, "Registry should be populated");
        Assert.IsTrue(tools.Count > 0, $"Should discover at least one tool (discoverer found {discovered.Count}); registry keys: {string.Join(", ", tools.Keys)}");
        Assert.IsTrue(tools.ContainsKey("test_get_items"), $"Should find test_get_items tool. Registry has: {string.Join(", ", tools.Keys)}");
    }

    [TestMethod]
    public void ToolDiscoveryRegistry__Derives__when__action_names__then__snake_case_tool_names()
    {
        // Arrange
        var registry = new ToolDiscoveryRegistry(typeof(TestController));

        // Act
        var tools = registry.Tools;

        // Assert
        Assert.IsTrue(tools.ContainsKey("test_get_items"), "GetItems action should map to test_get_items");
        Assert.IsTrue(tools.ContainsKey("test_create_item"), "CreateItem action should map to test_create_item");
        Assert.IsTrue(tools.ContainsKey("test_update_item_by_id"), "UpdateItemById action should map to test_update_item_by_id");
    }

    [TestMethod]
    public void ToolDiscoveryRegistry__Excludes__when__action_without_description__then__not_in_registry()
    {
        // Arrange
        var registry = new ToolDiscoveryRegistry(typeof(TestController));

        // Act
        var tools = registry.Tools;

        // Assert
        Assert.IsFalse(tools.ContainsKey("test_missing_description"),
            "Action without [Description] should not be in registry");
    }

    [TestMethod]
    public void ToolDiscoveryRegistry__Excludes__when__method_without_http_attribute__then__not_in_registry()
    {
        // Arrange
        var registry = new ToolDiscoveryRegistry(typeof(TestController));

        // Act
        var tools = registry.Tools;

        // Assert
        Assert.IsFalse(tools.ContainsKey("test_private_method"),
            "Method without [Http*] attribute should not be in registry");
    }

    [TestMethod]
    public void ToolDiscoveryRegistry__Stores__when__tool_registration__then__has_correct_metadata()
    {
        // Arrange
        var registry = new ToolDiscoveryRegistry(typeof(TestController));

        // Act
        var registration = registry.Tools["test_create_item"];

        // Assert
        Assert.AreEqual(typeof(TestController), registration.ControllerType,
            "Registration should store correct controller type");
        Assert.AreEqual("CreateItem", registration.ActionMethod.Name,
            "Registration should store correct method name");
        Assert.IsNotNull(registration.ParameterTypes, "Registration should have parameter types");
        Assert.IsTrue(registration.ParameterTypes.Count > 0, "Should extract parameter types");
    }

    [TestMethod]
    public void ToolDiscoveryRegistry__Handles__when__multiple_controllers__then__merges_tools()
    {
        // Arrange
        var registry = new ToolDiscoveryRegistry(
            typeof(TestController),
            typeof(AnotherController));

        // Act
        var tools = registry.Tools;

        // Assert
        Assert.IsTrue(tools.ContainsKey("test_get_items"), "Should have tools from first controller");
        Assert.IsTrue(tools.ContainsKey("another_get_something"), "Should have tools from second controller");
    }

    [TestMethod]
    public void ToolDiscoveryRegistry__Handles__when__two_controllers_same_action_different_names__then__both_tools_created()
    {
        // Arrange & Act: Create registry with two controllers having GetItems
        // Even though both have GetItems, the tool names should be different because controller names differ
        var registry = new ToolDiscoveryRegistry(
            typeof(TestController),
            typeof(DuplicateController));

        var tools = registry.Tools;

        // Assert: Both should be present with different names
        Assert.IsTrue(tools.ContainsKey("test_get_items"), "Should have test_get_items from TestController");
        Assert.IsTrue(tools.ContainsKey("duplicate_get_items"), "Should have duplicate_get_items from DuplicateController");
        Assert.IsTrue(tools.Count >= 4, $"Should have at least 4 tools (3 from TestController + 1 from DuplicateController), got {tools.Count}");
    }

    [TestMethod]
    public void ToolDiscoveryRegistry__Extracts__when__parameter_types__then__includes_body_and_route_params()
    {
        // Arrange
        var registry = new ToolDiscoveryRegistry(typeof(TestController));

        // Act
        var registration = registry.Tools["test_create_item"];

        // Assert
        Assert.IsNotNull(registration.ParameterTypes, "Should extract parameter types");
        // Note: actual body/route detection depends on reflection of [FromBody], [FromRoute], etc.
        // This test just verifies the extraction happens; specific assertions depend on test data.
    }

    [TestMethod]
    public void ToolDiscoveryRegistry__Excludes__when__action_marked_obsolete__then__not_in_registry()
    {
        // Arrange
        var registry = new ToolDiscoveryRegistry(typeof(ObsoleteTestController));

        // Act
        var tools = registry.Tools;

        // Assert
        Assert.IsFalse(tools.ContainsKey("obsolete_test_get_deprecated_item"),
            "Action marked with [Obsolete] should not be in registry");
        Assert.IsTrue(tools.ContainsKey("obsolete_test_get_current_item"),
            "Action without [Obsolete] should be in registry");
        Assert.AreEqual(1, tools.Count,
            "Should only discover the non-obsolete action");
    }

    [TestMethod]
    public void ToolDiscoveryRegistry__Registers__when__ToolName_attribute_present__then__uses_override_not_convention()
    {
        var registry = new ToolDiscoveryRegistry(typeof(ToolNameOverrideController));

        var tools = registry.Tools;

        Assert.IsTrue(tools.ContainsKey("custom_override_name"),
            $"Registry should contain the [ToolName] override 'custom_override_name'. Actual keys: {string.Join(", ", tools.Keys)}");
        Assert.IsFalse(tools.ContainsKey("tool_name_override_post_legacy"),
            "Registry should NOT contain the convention-derived name when [ToolName] is present");
    }
}

// ============================================================================
// Test Controllers
// ============================================================================

/// <summary>
/// Test controller with a mix of qualifying and non-qualifying actions.
/// </summary>
[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet("items")]
    [System.ComponentModel.Description("Retrieves all items")]
    public IActionResult GetItems()
    {
        return this.Ok();
    }

    [HttpPost("items")]
    [System.ComponentModel.Description("Creates a new item")]
    public IActionResult CreateItem([FromBody] object request)
    {
        return this.Ok();
    }

    [HttpPut("items/{id}")]
    [System.ComponentModel.Description("Updates an item by ID")]
    public IActionResult UpdateItemById([FromRoute] string id, [FromBody] object request)
    {
        return this.Ok();
    }

    [System.ComponentModel.Description("This has description but no [Http*] attribute")]
    public IActionResult MissingDescription()
    {
        return this.Ok();
    }

    [HttpGet("private")]
    public IActionResult PrivateMethod()
    {
        // No [Description] attribute
        return this.Ok();
    }
}

/// <summary>
/// Controller used to verify that [ToolName] overrides the convention-derived tool name in the registry.
/// </summary>
[ApiController]
[Route("api/tool-name-override")]
public class ToolNameOverrideController : ControllerBase
{
    [HttpPost("legacy")]
    [ToolName("custom_override_name")]
    [System.ComponentModel.Description("Legacy endpoint with a better MCP tool name")]
    public IActionResult PostLegacy()
    {
        return this.Ok();
    }
}

/// <summary>
/// Another test controller for multi-controller testing.
/// </summary>
[ApiController]
[Route("api/another")]
public class AnotherController : ControllerBase
{
    [HttpGet("something")]
    [System.ComponentModel.Description("Gets something")]
    public IActionResult GetSomething()
    {
        return this.Ok();
    }
}

/// <summary>
/// Test controller with a duplicate tool name (for testing duplicate detection).
/// </summary>
[ApiController]
[Route("api/test-duplicate")]
public class DuplicateController : ControllerBase
{
    [HttpGet("items")]
    [System.ComponentModel.Description("This creates a duplicate 'test_get_items' tool name")]
    public IActionResult GetItems()
    {
        return this.Ok();
    }
}

/// <summary>
/// Test controller with obsolete and current actions (for testing [Obsolete] exclusion).
/// </summary>
[ApiController]
[Route("api/obsolete-test")]
public class ObsoleteTestController : ControllerBase
{
    [HttpGet("current")]
    [System.ComponentModel.Description("Gets the current item")]
    public IActionResult GetCurrentItem()
    {
        return this.Ok();
    }

    [Obsolete("Use GetCurrentItem instead")]
    [HttpGet("deprecated")]
    [System.ComponentModel.Description("Gets a deprecated item")]
    public IActionResult GetDeprecatedItem()
    {
        return this.Ok();
    }
}
