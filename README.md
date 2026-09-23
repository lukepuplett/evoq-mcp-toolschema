# Evoq.Mcp.ToolSchema

Generates MCP (Model Context Protocol) `tools/list` definitions — `name`, `description`, and JSON Schema `inputSchema` — by reflecting over ASP.NET-style controller actions.

No dependency on ASP.NET Core itself: HTTP method and model-binding attributes (`[HttpGet]`, `[FromBody]`, etc.) are matched by type name, so any controller-shaped API can be reflected without this library referencing `Microsoft.AspNetCore.Mvc`.

## Usage

```csharp
var generator = new McpToolDefinitionsBatchGenerator();
var tools = generator.GenerateForSurface(typeof(OrdersApiController), surfaceName: "orders");
```

A qualifying action needs both an `[Http*]` attribute and a `[Description]` attribute. Attach `[BotSurface("orders")]` to a controller or action to scope it to a surface; `[ExcludeFromTools]` removes it from discovery entirely.

## Surfaces: exposing different tool sets to different callers

A "surface" is just a string name (`"orders"`, `"widgets"`, `"admin"`, ...) you assign to
actions with `[BotSurface("...")]`. It's the mechanism for exposing **different MCP tool sets to
different callers** from the same set of controllers — one API, multiple tool lists depending on
who's asking.

`GenerateForSurface(controllerType, surfaceName)` only returns tools for actions tagged with that
surface, so your `tools/list` handler decides which tool set a given caller sees by choosing which
surface(s) to generate for — e.g. by role, subscription tier, or MCP client identity:

```csharp
var surfacesForThisCaller = caller.IsAdmin
    ? new[] { "orders", "widgets", "admin" }
    : new[] { "orders", "widgets" };

var tools = surfacesForThisCaller
    .SelectMany(surface => generator.GenerateForSurface(controllerTypes, surface))
    .ToList();
```

An action can belong to more than one surface (`[BotSurface("orders", "widgets")]` or repeated
attributes), and a surface can optionally be restricted to one channel (`[BotSurface("orders",
channel: "MCP")]`) if the same controller also serves a non-MCP integration (e.g. a chat bot)
that should see a different tool subset.

The library only filters by surface name — deciding *which* surfaces a given caller is entitled to
is your application's policy, not something this library opines on.

## Attributes

Every action needs `[Http*]` + `[Description]` to qualify at all (that's the two-attribute floor —
see Usage above). Everything below is optional, on top of that floor.

### `[BotSurface]` — which tool set(s) an action belongs to

Covered in full above — see [Surfaces](#surfaces-exposing-different-tool-sets-to-different-callers).
Controller- or method-level, repeatable, optionally channel-scoped.

### `[ExcludeFromTools]` — remove an action (or whole controller) from discovery

Applied at class level, every action in the controller is excluded; applied at method level, only
that action is. An optional reason string documents *why*, for code review — it has no runtime
effect.

```csharp
[ExcludeFromTools("Requires elevated privileges; not safe to expose to an LLM caller")]
[HttpDelete("{id}/purge")]
[Description("Permanently purges an account")]
public IActionResult PurgeAccount(string id) { ... }
```

Effect: `PurgeAccount` never appears in `tools/list`, regardless of `[BotSurface]`.

### `[ToolHints]` — override the safety annotations emitted for a tool

Every tool's `annotations` (`readOnlyHint`, `destructiveHint`, `openWorldHint`, `idempotentHint`)
start from a convention derived from the HTTP verb:

| Verb          | readOnly | destructive | openWorld | idempotent |
|---------------|----------|-------------|-----------|------------|
| `[HttpGet]`   | `true`   | `false`     | `false`   | `false`    |
| `[HttpDelete]`| `false`  | `true`      | `false`   | `false`    |
| anything else | `false`  | `false`     | `false`   | `false`    |

`[ToolHints]` **ORs** on top of that — it can only raise a flag, never lower one derived from the
verb:

```csharp
[HttpPost("notify")]
[ToolHints(openWorld: true)]           // calls out to an external service
[Description("Sends a notification to an external system")]
public IActionResult SendNotification([FromBody] NotifyRequest request) { ... }
// -> annotations: { readOnlyHint: false, destructiveHint: false, openWorldHint: true, idempotentHint: false }

[HttpPost("delete/{id}")]
[ToolHints(destructive: true)]         // POST that deletes, so the GET/DELETE convention can't catch it
[Description("Deletes an item permanently")]
public IActionResult DeleteItem(string id) { ... }
// -> annotations: { readOnlyHint: false, destructiveHint: true, openWorldHint: false, idempotentHint: false }
```

### `[ToolName]` — override the convention-derived tool name

Without it, the tool name is `{controller}_{action}` in snake_case, with `Controller`/`Api`/`Async`
suffixes stripped (`OrdersApiController.SubmitFeedback` → `orders_submit_feedback`). With it, the
given name is used verbatim — useful when the convention would produce something awkward, or when
you need a stable name that survives a method rename:

```csharp
[HttpPost("feedback")]
[ToolName("orders_submit_feedback")]
[Description("Submits feedback about an order")]
public IActionResult PostFeedbackModern([FromBody] SubmitFeedbackRequest request) { ... }
// -> name: "orders_submit_feedback" (not "orders_post_feedback_modern")
```

### `[ExactlyOneOf]` / `[AtLeastOneOf]` — cross-field constraints on a request DTO

Class-level, on the request DTO type (not the action). Property names are C# names; the emitted
JSON Schema uses their camelCase JSON names. `ExactlyOneOf` emits `oneOf` (exactly one of the
listed properties must be present); `AtLeastOneOf` emits `anyOf` (one or more). Constrained
properties are removed from the top-level `required` array — the `allOf` entry expresses the
constraint instead:

```csharp
[ExactlyOneOf(nameof(TargetKey), nameof(TargetName))]
public sealed class LinkWidgetRequest
{
    public string? TargetKey { get; set; }
    public string? TargetName { get; set; }
}
```

```json
{
  "properties": {
    "targetKey": { "type": "string" },
    "targetName": { "type": "string" }
  },
  "allOf": [
    { "oneOf": [ { "required": ["targetKey"] }, { "required": ["targetName"] } ] }
  ]
}
```

Both attributes are repeatable on the same type for independent constraint groups (e.g. one
`AtLeastOneOf` pair and one `ExactlyOneOf` pair on the same DTO, as in a rename-with-locators
request).

All five non-surface attributes above are exercised end-to-end by `GoldenJsonOutputTests` and at
the unit level by `McpToolDefinitionBuilderTests`/`InputSchemaBuilderTests`/`QualifyingActionDiscovererTests`.

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

**Non-body parameters** — anything without `[FromBody]`, regardless of whether it carries
`[FromRoute]`/`[FromQuery]` or no binding attribute at all — may be a bare primitive: `string`,
any integer/floating-point/`decimal` type, `bool`, `DateTime`, `Guid`, or an enum, including their
nullable forms (`int?`, `Guid?`, ...). Each becomes its own named schema property, keyed by the C#
parameter name:

```csharp
[HttpGet("find")]
public IActionResult FindWidgets(Guid ownerId, DateTime? since, bool includeArchived, WidgetStatus status)
// -> properties: { ownerId: {type: string, format: uuid}, since: {type: string, format: date-time},
//                  includeArchived: {type: boolean}, status: {type: string, enum: [...]} }
```

Covered end-to-end by `GoldenJsonOutputTests` (asserts the exact generated JSON for a mixed-type,
unannotated parameter list) and by `InputSchemaBuilderTests` for each type individually.

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

This mirrors a common convention for well-behaved ASP.NET-style APIs: every `[FromBody]` parameter
is a `*Request`/`*Locator`/`*Config` record or class, never a primitive.
