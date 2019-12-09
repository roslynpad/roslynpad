using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Build
{
    internal class ExecutionHostParameters
    {
        public ExecutionHostParameters(
            string buildPath,
            ImmutableArray<string> imports,
            ImmutableArray<string> disabledDiagnostics,
            string workingDirectory,
            OptimizationLevel optimizationLevel = OptimizationLevel.Debug,
            bool checkOverflow = false,
            bool allowUnsafe = true)
        {
            BuildPath = buildPath;
            Imports = imports;
            DisabledDiagnostics = disabledDiagnostics;
            WorkingDirectory = workingDirectory;
            OptimizationLevel = optimizationLevel;
            CheckOverflow = checkOverflow;
            AllowUnsafe = allowUnsafe;
        }

        public string BuildPath { get; }
        public ImmutableArray<string> Imports { get; set; }
        public ImmutableArray<string> DisabledDiagnostics { get; }
        public string WorkingDirectory { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public bool CheckOverflow { get; }
        public bool AllowUnsafe { get; }
    }
}