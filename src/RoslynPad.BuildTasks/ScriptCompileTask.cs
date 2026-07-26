using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.BuildTasks;

/// <summary>
/// Compiles C# script (<c>.csx</c>) sources in place of <c>Csc</c>. The script csproj overrides
/// the <c>CoreCompile</c> target to invoke this task, so the rest of the SDK build pipeline
/// (reference resolution, deps.json/runtimeconfig generation, copy-local) runs stock.
/// </summary>
public sealed class ScriptCompileTask : ITask
{
    public IBuildEngine? BuildEngine { get; set; }
    public ITaskHost? HostObject { get; set; }

    [Required]
    public ITaskItem[] Sources { get; set; } = [];

    public ITaskItem[] References { get; set; } = [];

    [Required]
    public string OutputAssembly { get; set; } = "";

    public bool Optimize { get; set; }
    public bool CheckOverflow { get; set; }
    public bool AllowUnsafe { get; set; } = true;
    public bool Prefer32Bit { get; set; }
    public string? Imports { get; set; }
    public string? NoWarn { get; set; }
    public string? WorkingDirectory { get; set; }

    public bool Execute()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview, kind: SourceCodeKind.Script);

        var trees = new List<SyntaxTree>(Sources.Length);
        foreach (var source in Sources)
        {
            var path = source.GetMetadata("FullPath");
            using var stream = File.OpenRead(path);
            trees.Add(CSharpSyntaxTree.ParseText(SourceText.From(stream), parseOptions, path));
        }

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.ConsoleApplication,
            scriptClassName: "Program",
            usings: SplitList(Imports),
            optimizationLevel: Optimize ? OptimizationLevel.Release : OptimizationLevel.Debug,
            checkOverflow: CheckOverflow,
            allowUnsafe: AllowUnsafe,
            platform: Prefer32Bit ? Platform.AnyCpu32BitPreferred : Platform.AnyCpu,
            warningLevel: 4,
            deterministic: true,
            sourceReferenceResolver: string.IsNullOrEmpty(WorkingDirectory)
                ? SourceFileResolver.Default
                : new SourceFileResolver([], WorkingDirectory),
            assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
            nullableContextOptions: NullableContextOptions.Enable,
            specificDiagnosticOptions: GetSuppressedDiagnostics());

        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(OutputAssembly),
            trees,
            References.Select(r => (MetadataReference)MetadataReference.CreateFromFile(r.GetMetadata("FullPath"))),
            compilationOptions);

        using var peStream = OpenWrite(OutputAssembly);
        using var pdbStream = OpenWrite(Path.ChangeExtension(OutputAssembly, ".pdb"));
        var result = compilation.Emit(
            peStream,
            pdbStream,
            options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));

        foreach (var diagnostic in result.Diagnostics)
        {
            Log(diagnostic);
        }

        return result.Success;

        // Truncate rather than File.OpenWrite, which keeps the tail of a previously larger file
        static FileStream OpenWrite(string path) =>
            new(path, FileMode.Create, FileAccess.Write);
    }

    private IEnumerable<KeyValuePair<string, ReportDiagnostic>> GetSuppressedDiagnostics() =>
        SplitList(NoWarn)
            .Select(id => new KeyValuePair<string, ReportDiagnostic>(
                int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                    ? $"CS{number:D4}"
                    : id,
                ReportDiagnostic.Suppress));

    private static IEnumerable<string> SplitList(string? value) =>
        (value ?? "").Split([';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0);

    private void Log(Diagnostic diagnostic)
    {
        if (diagnostic.Severity is not (DiagnosticSeverity.Error or DiagnosticSeverity.Warning))
        {
            return;
        }

        var lineSpan = diagnostic.Location.GetLineSpan();
        var file = lineSpan.Path is { Length: > 0 } path ? path : OutputAssembly;
        var start = lineSpan.StartLinePosition;
        var end = lineSpan.EndLinePosition;
        var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);

        if (diagnostic.Severity == DiagnosticSeverity.Error)
        {
            BuildEngine?.LogErrorEvent(new BuildErrorEventArgs(
                subcategory: null, diagnostic.Id, file,
                start.Line + 1, start.Character + 1, end.Line + 1, end.Character + 1,
                message, helpKeyword: null, senderName: nameof(ScriptCompileTask)));
        }
        else
        {
            BuildEngine?.LogWarningEvent(new BuildWarningEventArgs(
                subcategory: null, diagnostic.Id, file,
                start.Line + 1, start.Character + 1, end.Line + 1, end.Character + 1,
                message, helpKeyword: null, senderName: nameof(ScriptCompileTask)));
        }
    }
}
