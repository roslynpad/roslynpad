using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Globalization",
    "CA1305:Specify IFormatProvider",
    Justification = "Upstream Roslyn logging intentionally uses the current culture.",
    Scope = "member",
    Target = "~M:Microsoft.CodeAnalysis.CSharp.DecompiledSource.AssemblyResolver.Log(System.String,System.Object[])")]
