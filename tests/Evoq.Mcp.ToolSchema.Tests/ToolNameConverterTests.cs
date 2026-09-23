#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Evoq.Mcp.ToolSchema;

namespace Evoq.Mcp.ToolSchema.Tests;

[TestClass]
public class ToolNameConverterTests
{
    [TestMethod]
    public void ToolNameConverter__ToToolName__when__JournalApiController_GetEntries__then__journal_get_entries()
    {
        var result = ToolNameConverter.ToToolName("JournalApiController", "GetEntries");

        Assert.AreEqual("journal_get_entries", result, "Should strip 'Controller', lowercase both parts, and join with underscore");
    }

    [TestMethod]
    public void ToolNameConverter__ToToolName__when__OutboundTimesheetsApiController_ListTimesheets__then__outbound_timesheets_list_timesheets()
    {
        var result = ToolNameConverter.ToToolName("OutboundTimesheetsApiController", "ListTimesheets");

        Assert.AreEqual("outbound_timesheets_list_timesheets", result, "Should handle multi-word controller and action names");
    }

    [TestMethod]
    public void ToolNameConverter__ToToolName__when__AccountApiController_SetDocLocaleAsync__then__account_set_doc_locale()
    {
        var result = ToolNameConverter.ToToolName("AccountApiController", "SetDocLocaleAsync");

        Assert.AreEqual("account_set_doc_locale", result, "Should strip both 'Controller' and 'Async'");
    }

    [TestMethod]
    public void ToolNameConverter__ToToolName__when__MyTimesheetsController_RetractAsync__then__my_timesheets_retract()
    {
        var result = ToolNameConverter.ToToolName("MyTimesheetsController", "RetractAsync");

        Assert.AreEqual("my_timesheets_retract", result, "Should handle controller names without 'Api' suffix");
    }

    [TestMethod]
    public void ToolNameConverter__ToToolName__when__FooApiController_BarBazQux__then__foo_bar_baz_qux()
    {
        var result = ToolNameConverter.ToToolName("FooApiController", "BarBazQux");

        Assert.AreEqual("foo_bar_baz_qux", result, "Should join any number of PascalCase words");
    }

    [TestMethod]
    public void ToolNameConverter__ToToolName__when__preserves_embedded_caps_in_action()
    {
        var result = ToolNameConverter.ToToolName("ApiController", "GetOAuthToken");

        Assert.AreEqual("api_get_o_auth_token", result, "PascalCase words are separated by lowercase");
    }

    [TestMethod]
    public void ToolNameConverter__ToToolName__when__no_controller_suffix__then__still_lowercases()
    {
        var result = ToolNameConverter.ToToolName("Journal", "GetEntries");

        Assert.AreEqual("journal_get_entries", result, "Should handle controller names without 'Controller' suffix");
    }

    [TestMethod]
    public void ToolNameConverter__ToToolName__when__no_async_suffix__then__still_works()
    {
        var result = ToolNameConverter.ToToolName("AccountApiController", "SetDocLocale");

        Assert.AreEqual("account_set_doc_locale", result, "Should work with action names without 'Async'");
    }

    [TestMethod]
    public void ToolNameConverter__ToToolName__when__single_word_controller_and_action__then__minimal_names_work()
    {
        var result = ToolNameConverter.ToToolName("FooController", "Bar");

        Assert.AreEqual("foo_bar", result, "Should work with minimal names");
    }

    [TestMethod]
    public void ToolNameConverter__ToToolName__when__handles_controller_without_suffix()
    {
        var result = ToolNameConverter.ToToolName("Foo", "Bar");

        Assert.AreEqual("foo_bar", result, "Should handle controller names without any suffix");
    }

    // Typed overload tests (Type, string) — checks [ToolName] reflection and fallback

    [TestMethod]
    public void ToolNameConverter__ToToolName_typed__when__no_ToolName_attribute__then__uses_convention()
    {
        var result = ToolNameConverter.ToToolName(typeof(FakeApiController), nameof(FakeApiController.MyAction));

        Assert.AreEqual("fake_my_action", result, "Should fall back to convention when no [ToolName] attribute is present");
    }

    [TestMethod]
    public void ToolNameConverter__ToToolName_typed__when__ToolName_attribute_present__then__returns_override()
    {
        var result = ToolNameConverter.ToToolName(typeof(FakeWithOverrideApiController), nameof(FakeWithOverrideApiController.MyAction));

        Assert.AreEqual("custom_tool_name", result, "Should return the [ToolName] value when the attribute is present");
    }

    [TestMethod]
    public void ToolNameConverter__ToToolName_typed__when__ToolName_attribute_on_one_method_but_not_another__then__only_overrides_that_method()
    {
        var overridden = ToolNameConverter.ToToolName(typeof(FakeWithOverrideApiController), nameof(FakeWithOverrideApiController.MyAction));
        var conventional = ToolNameConverter.ToToolName(typeof(FakeWithOverrideApiController), nameof(FakeWithOverrideApiController.OtherAction));

        Assert.AreEqual("custom_tool_name", overridden, "Overridden method should use [ToolName] value");
        Assert.AreEqual("fake_with_override_other_action", conventional, "Unannotated method should use convention");
    }

    [TestMethod]
    public void ToolNameConverter__ToToolName__when__SenderOnboardingApiController_methods()
    {
        var result1 = ToolNameConverter.ToToolName("SenderOnboardingApiController", "PostCreateSenderAccount");
        var result2 = ToolNameConverter.ToToolName("SenderOnboardingApiController", "PostCreateSenderAccountViaWhatsApp");
        var result3 = ToolNameConverter.ToToolName("SenderOnboardingApiController", "CreateSenderAccount");
        var result4 = ToolNameConverter.ToToolName("SenderOnboardingApiController", "CreateSenderAccountViaWhatsApp");

        Assert.AreEqual("sender_onboarding_post_create_sender_account", result1);
        Assert.AreEqual("sender_onboarding_post_create_sender_account_via_whats_app", result2);
        Assert.AreEqual("sender_onboarding_create_sender_account", result3);
        Assert.AreEqual("sender_onboarding_create_sender_account_via_whats_app", result4);
    }
}

// Fake controllers used by typed-overload tests — defined outside the test class to avoid name collisions

internal class FakeApiController
{
    public void MyAction() { }
}

internal class FakeWithOverrideApiController
{
    [ToolName("custom_tool_name")]
    public void MyAction() { }

    public void OtherAction() { }
}
