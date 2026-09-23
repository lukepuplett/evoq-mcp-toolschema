#if NETSTANDARD2_1
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill required to use C# 9 record/init-only members when targeting netstandard2.1,
/// which lacks this type in its own runtime.
/// </summary>
internal static class IsExternalInit
{
}
#endif
