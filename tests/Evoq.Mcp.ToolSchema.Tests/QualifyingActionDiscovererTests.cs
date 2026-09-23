#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Evoq.Mcp.ToolSchema;
using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Evoq.Mcp.ToolSchema.Tests;

[TestClass]
public class QualifyingActionDiscovererTests
{
    private readonly QualifyingActionDiscoverer discoverer = new();

    [TestMethod]
    public void QualifyingActionDiscoverer__Discover__when__controller_with_qualifying_action__then__returns_that_action()
    {
        var results = discoverer.Discover(typeof(TestApiController));

        Assert.IsNotNull(results, "Should return results");
        Assert.IsTrue(results.Count > 0, "Should find at least one qualifying action");

        var qualifying = results.FirstOrDefault(r => r.ActionName == "GetItems");
        Assert.IsNotNull(qualifying.Method, "Should find GetItems action");
        Assert.AreEqual("Test", qualifying.ControllerName, "Should extract controller name, stripping both 'Controller' and 'Api' suffixes");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__Discover__when__action_without_description__then__excludes_it()
    {
        var results = discoverer.Discover(typeof(TestApiController));

        var unqualified = results.FirstOrDefault(r => r.ActionName == "NoDescription");
        Assert.IsNull(unqualified.Method, "Should not include action without [Description]");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__Discover__when__method_without_http_attribute__then__excludes_it()
    {
        var results = discoverer.Discover(typeof(TestApiController));

        var unqualified = results.FirstOrDefault(r => r.ActionName == "NotAnAction");
        Assert.IsNull(unqualified.Method, "Should not include method without [Http*] attribute");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__Discover__when__multiple_qualifying_actions__then__returns_all()
    {
        var results = discoverer.Discover(typeof(TestApiController));

        var count = results.Count(r => r.Method != null);
        Assert.IsTrue(count >= 2, "Should find multiple qualifying actions");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__Discover__when__action_marked_obsolete__then__excludes_it()
    {
        var results = discoverer.Discover(typeof(TestApiController));

        var obsolete = results.FirstOrDefault(r => r.ActionName == "ObsoleteAction");
        Assert.IsNull(obsolete.Method, "Should exclude [Obsolete] actions from discovery");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__Discover__when__action_marked_exclude_from_tools__then__excludes_it()
    {
        var results = discoverer.Discover(typeof(TestApiController));

        var excluded = results.FirstOrDefault(r => r.ActionName == "ExcludedAction");
        Assert.IsNull(excluded.Method, "Should exclude [ExcludeFromTools] actions from discovery");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__controller_marked_exclude_from_tools__then__excludes_all_actions()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerExcludedFromTools), "test-surface");

        Assert.AreEqual(0, results.Count, "Should exclude all actions in controller marked [ExcludeFromTools]");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__action_marked_exclude_from_tools__then__excludes_only_that_action()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithActionExclude), "test-surface");

        Assert.AreEqual(1, results.Count, "Should exclude only the action marked [ExcludeFromTools]");
        Assert.AreEqual("GetItems", results[0].ActionName, "Should find only GetItems action");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__Discover__when__extracts_correct_action_names__then__valid()
    {
        var results = discoverer.Discover(typeof(TestApiController));

        var actionNames = results.Select(r => r.ActionName).ToList();
        Assert.IsTrue(actionNames.Contains("GetItems"), "Should have GetItems action");
        Assert.IsTrue(actionNames.Contains("CreateItem"), "Should have CreateItem action");
    }

    // ========== Surface-scoped discovery tests ==========

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__controller_with_surface_attribute__then__returns_all_controller_actions()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithSurfaceAttribute), "test-surface");

        Assert.IsNotNull(results, "Should return results");
        Assert.AreEqual(2, results.Count, "Should find all 2 qualifying actions in controller with surface");

        var actionNames = results.Select(r => r.ActionName).ToList();
        Assert.IsTrue(actionNames.Contains("GetItems"), "Should have GetItems action");
        Assert.IsTrue(actionNames.Contains("CreateItem"), "Should have CreateItem action");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__action_with_surface_attribute__then__returns_only_that_action()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithActionSurface), "test-surface");

        Assert.IsNotNull(results, "Should return results");
        Assert.AreEqual(1, results.Count, "Should find only the 1 action with surface attribute");
        Assert.AreEqual("GetItems", results[0].ActionName, "Should find GetItems action with surface");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__surface_not_found__then__returns_empty()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithSurfaceAttribute), "non-existent-surface");

        Assert.IsNotNull(results, "Should return empty list, not null");
        Assert.AreEqual(0, results.Count, "Should return empty list when surface doesn't exist");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__action_overrides_controller_surface__then__uses_action_surface()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithMixedSurfaces), "action-specific");

        Assert.AreEqual(1, results.Count, "Should find only 1 action with action-specific surface");
        Assert.AreEqual("GetSpecial", results[0].ActionName, "Should find GetSpecial action");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__controller_and_action_surfaces_different__then__action_surface_takes_precedence()
    {
        // Controller has "controller-surface", GetSpecial has "action-specific"
        // Searching for "controller-surface" should NOT return GetSpecial
        var resultsForController = discoverer.DiscoverBySurface(typeof(ControllerWithMixedSurfaces), "controller-surface");

        var getSpecial = resultsForController.FirstOrDefault(r => r.ActionName == "GetSpecial");
        Assert.IsNull(getSpecial.Method, "Should NOT find GetSpecial when searching for controller-surface (action overrides controller)");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__two_pot_rule_still_enforced__then__excludes_non_qualifying_methods()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithSurfaceAttribute), "test-surface");

        // NoDescription has surface but no [Description] - should be excluded
        var noDescription = results.FirstOrDefault(r => r.ActionName == "NoDescription");
        Assert.IsNull(noDescription.Method, "Should exclude action without [Description] even if it has [BotSurface]");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__surface_name_null__then__throws_argument_exception()
    {
        discoverer.DiscoverBySurface(typeof(TestApiController), null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__surface_name_empty__then__throws_argument_exception()
    {
        discoverer.DiscoverBySurface(typeof(TestApiController), "");
    }

    // ========== Channel-scoped discovery tests ==========

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__no_channel_specified_and_action_unrestricted__then__returns_action()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithChannelSpecificActions), "test-surface", channelName: null);

        var unrestrictedAction = results.FirstOrDefault(r => r.ActionName == "UnrestrictedAction");
        Assert.IsNotNull(unrestrictedAction.Method, "Should find action with no channel restriction when no channel filter specified");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__channel_mcp_and_action_has_mcp_channel__then__returns_action()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithChannelSpecificActions), "test-surface", channelName: "MCP");

        var mcpAction = results.FirstOrDefault(r => r.ActionName == "McpOnlyAction");
        Assert.IsNotNull(mcpAction.Method, "Should find action restricted to MCP channel when discovering for MCP");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__channel_whatsapp_and_action_has_mcp_channel__then__excludes_action()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithChannelSpecificActions), "test-surface", channelName: "WhatsApp");

        var mcpAction = results.FirstOrDefault(r => r.ActionName == "McpOnlyAction");
        Assert.IsNull(mcpAction.Method, "Should NOT find action restricted to MCP channel when discovering for WhatsApp");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__channel_mcp_and_action_unrestricted__then__returns_action()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithChannelSpecificActions), "test-surface", channelName: "MCP");

        var unrestrictedAction = results.FirstOrDefault(r => r.ActionName == "UnrestrictedAction");
        Assert.IsNotNull(unrestrictedAction.Method, "Should find unrestricted action when discovering for any channel");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__multiple_channel_attributes_and_channel_matches__then__returns_action()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithChannelSpecificActions), "test-surface", channelName: "WhatsApp");

        var multiChannelAction = results.FirstOrDefault(r => r.ActionName == "MultiChannelAction");
        Assert.IsNotNull(multiChannelAction.Method, "Should find action with multiple channels when one matches (WhatsApp)");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__multiple_channel_attributes_and_mcp_matches__then__returns_action()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithChannelSpecificActions), "test-surface", channelName: "MCP");

        var multiChannelAction = results.FirstOrDefault(r => r.ActionName == "MultiChannelAction");
        Assert.IsNotNull(multiChannelAction.Method, "Should find action with multiple channels when one matches (MCP)");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__channel_signal_and_action_excludes_signal__then__excludes_action()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithChannelSpecificActions), "test-surface", channelName: "Signal");

        var mcpAction = results.FirstOrDefault(r => r.ActionName == "McpOnlyAction");
        var multiChannelAction = results.FirstOrDefault(r => r.ActionName == "MultiChannelAction");

        Assert.IsNull(mcpAction.Method, "Should exclude MCP-only action when discovering for Signal");
        Assert.IsNull(multiChannelAction.Method, "Should exclude multi-channel action when discovering for unsupported channel (Signal)");
    }

    [TestMethod]
    public void QualifyingActionDiscoverer__DiscoverBySurface__when__controller_has_description_but_no_class_level_surface_and_method_has_surface__then__discovers_action()
    {
        var results = discoverer.DiscoverBySurface(typeof(ControllerWithMethodLevelSurface), "test-surface", channelName: "WhatsApp");

        var action = results.FirstOrDefault(r => r.ActionName == "CreateAccount");
        Assert.IsNotNull(action.Method, "Should discover action with method-level [BotSurface] even when controller has no class-level [BotSurface]");
        Assert.AreEqual("CreateAccount", action.ActionName, "Should correctly identify method-level surface action");
    }

    // Test controller with surface attribute at controller level
    [ApiController]
    [Route("api/test")]
    private class TestApiController : ControllerBase
    {
        [HttpGet]
        [System.ComponentModel.Description("Retrieves list of items")]
        public IActionResult GetItems()
        {
            return Ok();
        }

        [HttpPost]
        [System.ComponentModel.Description("Creates a new item")]
        public IActionResult CreateItem([FromBody] object item)
        {
            return Created("", item);
        }

        [HttpGet("{id}")]
        [System.ComponentModel.Description("Gets item by ID")]
        public IActionResult GetItemById(string id)
        {
            return Ok();
        }

        [HttpGet]
        public IActionResult NoDescription()
        {
            return Ok();
        }

        [System.ComponentModel.Description("This has description but no HTTP attribute")]
        public void NotAnAction()
        {
        }

        [HttpGet("old")]
        [System.ComponentModel.Description("Deprecated action")]
        [System.Obsolete("This action is deprecated")]
        public IActionResult ObsoleteAction()
        {
            return Ok();
        }

        [HttpDelete("{id}")]
        [System.ComponentModel.Description("Deletes an item")]
        [ExcludeFromTools("This endpoint requires elevated privileges")]
        public IActionResult ExcludedAction(string id)
        {
            return NoContent();
        }
    }

    // Test controller with [BotSurface] at controller level
    [ApiController]
    [Route("api/surface-test")]
    [BotSurface("test-surface")]
    private class ControllerWithSurfaceAttribute : ControllerBase
    {
        [HttpGet]
        [System.ComponentModel.Description("Retrieves list of items")]
        public IActionResult GetItems()
        {
            return Ok();
        }

        [HttpPost]
        [System.ComponentModel.Description("Creates a new item")]
        public IActionResult CreateItem([FromBody] object item)
        {
            return Created("", item);
        }

        [HttpGet]
        // Missing [Description] - should be excluded even with [BotSurface]
        public IActionResult NoDescription()
        {
            return Ok();
        }
    }

    // Test controller with [BotSurface] at action level only
    [ApiController]
    [Route("api/action-surface-test")]
    private class ControllerWithActionSurface : ControllerBase
    {
        [HttpGet]
        [System.ComponentModel.Description("Retrieves list of items")]
        [BotSurface("test-surface")]
        public IActionResult GetItems()
        {
            return Ok();
        }

        [HttpPost]
        [System.ComponentModel.Description("Creates a new item")]
        public IActionResult CreateItem([FromBody] object item)
        {
            return Created("", item);
        }
    }

    // Test controller with mixed surfaces (controller-level + action-level overrides)
    [ApiController]
    [Route("api/mixed-surface-test")]
    [BotSurface("controller-surface")]
    private class ControllerWithMixedSurfaces : ControllerBase
    {
        [HttpGet]
        [System.ComponentModel.Description("Retrieves list of items")]
        public IActionResult GetItems()
        {
            return Ok();
        }

        [HttpGet("special")]
        [System.ComponentModel.Description("Gets special item")]
        [BotSurface("action-specific")]  // This overrides controller-surface
        public IActionResult GetSpecial()
        {
            return Ok();
        }
    }

    // Test controller with [ExcludeFromTools] at controller level
    [ApiController]
    [Route("api/excluded-controller")]
    [BotSurface("test-surface")]
    [ExcludeFromTools("This controller is for internal use only")]
    private class ControllerExcludedFromTools : ControllerBase
    {
        [HttpGet]
        [System.ComponentModel.Description("Retrieves list of items")]
        public IActionResult GetItems()
        {
            return Ok();
        }

        [HttpPost]
        [System.ComponentModel.Description("Creates a new item")]
        public IActionResult CreateItem([FromBody] object item)
        {
            return Created("", item);
        }
    }

    // Test controller with [ExcludeFromTools] at action level
    [ApiController]
    [Route("api/action-exclude")]
    [BotSurface("test-surface")]
    private class ControllerWithActionExclude : ControllerBase
    {
        [HttpGet]
        [System.ComponentModel.Description("Retrieves list of items")]
        public IActionResult GetItems()
        {
            return Ok();
        }

        [HttpPost]
        [System.ComponentModel.Description("Creates a new item")]
        [ExcludeFromTools("This action requires special authentication")]
        public IActionResult CreateItem([FromBody] object item)
        {
            return Created("", item);
        }
    }

    // Test controller with channel-specific surface attributes
    [ApiController]
    [Route("api/channel-test")]
    private class ControllerWithChannelSpecificActions : ControllerBase
    {
        /// <summary>
        /// Available on all channels (no channel restriction).
        /// </summary>
        [HttpGet("unrestricted")]
        [System.ComponentModel.Description("Available on all channels")]
        [BotSurface("test-surface")]
        public IActionResult UnrestrictedAction()
        {
            return Ok();
        }

        /// <summary>
        /// Available only on MCP channel.
        /// </summary>
        [HttpGet("mcp-only")]
        [System.ComponentModel.Description("Available only on MCP")]
        [BotSurface("test-surface", channel: "MCP")]
        public IActionResult McpOnlyAction()
        {
            return Ok();
        }

        /// <summary>
        /// Available on multiple channels (MCP and WhatsApp).
        /// </summary>
        [HttpPost("multi-channel")]
        [System.ComponentModel.Description("Available on MCP and WhatsApp")]
        [BotSurface("test-surface", channel: "MCP")]
        [BotSurface("test-surface", channel: "WhatsApp")]
        public IActionResult MultiChannelAction([FromBody] object data)
        {
            return Created("", data);
        }
    }

    // Test controller with class-level [Description] but no class-level [BotSurface]
    // Surface filtering is done at method level only
    [ApiController]
    [Route("api/method-level-surface-test")]
    [System.ComponentModel.Description("Manages onboarding for new accounts")]
    private class ControllerWithMethodLevelSurface : ControllerBase
    {
        /// <summary>
        /// Creates account with WhatsApp channel restriction.
        /// </summary>
        [HttpPost("create")]
        [System.ComponentModel.Description("Creates a new account via WhatsApp")]
        [BotSurface("test-surface", channel: "WhatsApp")]
        public IActionResult CreateAccount([FromBody] object data)
        {
            return Created("", data);
        }

        /// <summary>
        /// Updates account with no channel restriction.
        /// </summary>
        [HttpPost("update")]
        [System.ComponentModel.Description("Updates an existing account")]
        [BotSurface("test-surface")]
        public IActionResult UpdateAccount([FromBody] object data)
        {
            return Ok(data);
        }
    }
}
