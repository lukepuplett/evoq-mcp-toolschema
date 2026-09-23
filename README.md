# Evoq.Mcp.ToolSchema

Generates MCP (Model Context Protocol) `tools/list` definitions — `name`, `description`, and JSON Schema `inputSchema` — by reflecting over ASP.NET-style controller actions.

No dependency on ASP.NET Core itself: HTTP method and model-binding attributes (`[HttpGet]`, `[FromBody]`, etc.) are matched by type name, so any controller-shaped API can be reflected without this library referencing `Microsoft.AspNetCore.Mvc`.

## Usage

```csharp
var generator = new McpToolDefinitionsBatchGenerator();
var tools = generator.GenerateForSurface(typeof(JournalApiController), surfaceName: "journal");
```

A qualifying action needs both an `[Http*]` attribute and a `[Description]` attribute. Attach `[BotSurface("journal")]` to a controller or action to scope it to a surface; `[ExcludeFromTools]` removes it from discovery entirely.

## Surfaces: exposing different tool sets to different callers

A "surface" is just a string name (`"journal"`, `"workflows"`, `"account"`, ...) you assign to
actions with `[BotSurface("...")]`. It's the mechanism for exposing **different MCP tool sets to
different callers** from the same set of controllers — one API, multiple tool lists depending on
who's asking.

`GenerateForSurface(controllerType, surfaceName)` only returns tools for actions tagged with that
surface, so your `tools/list` handler decides which tool set a given caller sees by choosing which
surface(s) to generate for — e.g. by role, subscription tier, or MCP client identity:

```csharp
var surfacesForThisCaller = caller.IsAdmin
    ? new[] { "journal", "workflows", "account" }
    : new[] { "journal", "account" };

var tools = surfacesForThisCaller
    .SelectMany(surface => generator.GenerateForSurface(controllerTypes, surface))
    .ToList();
```

An action can belong to more than one surface (`[BotSurface("journal", "timesheets")]` or repeated
attributes), and a surface can optionally be restricted to one channel (`[BotSurface("journal",
channel: "MCP")]`) if the same controller also serves a non-MCP integration (e.g. a WhatsApp bot)
that should see a different tool subset.

The library only filters by surface name — deciding *which* surfaces a given caller is entitled to
is your application's policy, not something this library opines on.

## Attributes

- `BotSurfaceAttribute` — assigns a controller/action to one or more MCP surfaces, optionally restricted to a channel.
- `ExcludeFromToolsAttribute` — hides a controller/action from MCP tool discovery.
- `ToolHintsAttribute` — overrides the convention-derived `readOnly`/`destructive`/`openWorld`/`idempotent` annotation hints.
- `ToolNameAttribute` — overrides the convention-derived snake_case tool name.
- `ExactlyOneOfAttribute` / `AtLeastOneOfAttribute` — class-level constraints on a request DTO, emitted as JSON Schema `oneOf`/`anyOf`.

## Wiring this into your API

This library only turns reflected controller actions into `tools/list` JSON. It does not serve an
HTTP endpoint itself — you write a thin MCP controller/handler that:

1. Calls `McpToolDefinitionsBatchGenerator.GenerateForSurface(...)` (or the lower-level
   `QualifyingActionDiscoverer` + `McpToolDefinitionBuilder`) to build the `tools` array for
   `tools/list`, typically once per surface and cached — reflection is not free.
2. On `tools/call`, resolves the tool name back to its controller action (e.g. via your own
   name → `MethodInfo` registry — see `ToolDiscoveryRegistry`) and invokes it, binding the
   caller-supplied `arguments` object the same way ASP.NET model binding would.

The library never touches the HTTP pipeline, request execution, or auth — that plumbing is yours.

## Constraint: every reflected action needs proper model binding — no bare primitives

The schema builder works by reflecting each parameter type's **public properties**. That only
produces a sensible schema when the parameter is a real request DTO — a `class` or `record` whose
public properties are the request's fields.

**Route/query parameters** may be bare primitives (`string`, `int`, `Guid`, `bool`, `DateTime`,
enums) — they're bound by parameter name into a single named schema property, e.g.:

```csharp
[HttpGet("{timesheetKey}/attachments/{fileName}")]
public IActionResult GetAttachment([FromRoute] string timesheetKey, [FromRoute] string fileName)
```

**`[FromBody]` parameters must always be a DTO**, never a bare primitive, enum, `Guid`, `DateTime`,
or a raw array/collection. Reflecting `System.String` or `List<T>` directly would expose their CLR
members (`Length`, `Chars`, `Capacity`, ...) as if they were request fields, which is wrong and
confusing. This library detects that case at discovery time and throws
`UnsupportedBodyParameterTypeException` rather than silently emitting a garbage schema:

```csharp
// Wrong — throws UnsupportedBodyParameterTypeException at discovery time
[HttpPost]
public IActionResult Rename([FromBody] string newName) { ... }

// Right — wrap it in a request DTO
public sealed record RenameRequest(string NewName);

[HttpPost]
public IActionResult Rename([FromBody] RenameRequest request) { ... }
```

This mirrors how every controller in Zipwire's own API is written: every `[FromBody]` parameter is
a `*Request`/`*Locator`/`*Config` record or class, never a primitive.
