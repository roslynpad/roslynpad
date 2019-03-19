using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Hosting
{
    internal class ExecutionHostParameters
    {
        public ExecutionHostParameters(
            ImmutableArray<string> compileReferences,
            ImmutableArray<string> runtimeReferences,
            ImmutableArray<string> directReferences,
            ImmutableArray<MetadataReference> frameworkReferences,
            ImmutableArray<string> imports,
            ImmutableArray<string> disabledDiagnostics,
            string workingDirectory,
            string globalPackageFolder,
            OptimizationLevel optimizationLevel = OptimizationLevel.Debug,
            bool checkOverflow = false,
            bool allowUnsafe = true)
        {
            NuGetCompileReferences = compileReferences;
            NuGetRuntimeReferences = runtimeReferences;
            DirectReferences = directReferences;
            FrameworkReferences = frameworkReferences;
            Imports = imports;
            DisabledDiagnostics = disabledDiagnostics;
            WorkingDirectory = workingDirectory;
            OptimizationLevel = optimizationLevel;
            CheckOverflow = checkOverflow;
            AllowUnsafe = allowUnsafe;
            GlobalPackageFolder = globalPackageFolder;
        }

        public ImmutableArray<string> NuGetCompileReferences { get; set; }
        public ImmutableArray<string> NuGetRuntimeReferences { get; set; }
        public ImmutableArray<string> DirectReferences { get; set; }
        public ImmutableArray<MetadataReference> FrameworkReferences { get; set; }
        public ImmutableArray<string> Imports { get; set; }
        public ImmutableArray<string> DisabledDiagnostics { get; }
        public string WorkingDirectory { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public bool CheckOverflow { get; }
        public bool AllowUnsafe { get; }
        public string GlobalPackageFolder { get; set; }
    }
}