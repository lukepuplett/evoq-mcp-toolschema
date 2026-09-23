#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Evoq.Mcp.ToolSchema;

/// <summary>
/// Metadata for one discovered tool: everything needed to generate its schema or invoke it.
/// </summary>
public readonly record struct ToolRegistration(
    Type ControllerType,
    MethodInfo ActionMethod,
    IReadOnlyList<(Type Type, bool IsBody)> ParameterTypes);
