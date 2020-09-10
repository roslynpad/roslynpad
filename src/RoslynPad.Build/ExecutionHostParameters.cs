using System.Collections.Immutable;

namespace RoslynPad.Build
{
    internal class ExecutionHostParameters
    {
        public ExecutionHostParameters(
            string buildPath,
            string nuGetConfigPath,
            ImmutableArray<string> imports,
            ImmutableArray<string> disabledDiagnostics,
            string workingDirectory,
            bool checkOverflow = false,
            bool allowUnsafe = true)
        {
            BuildPath = buildPath;
            NuGetConfigPath = nuGetConfigPath;
            Imports = imports;
            DisabledDiagnostics = disabledDiagnostics;
            WorkingDirectory = workingDirectory;
            CheckOverflow = checkOverflow;
            AllowUnsafe = allowUnsafe;
        }

        public string BuildPath { get; }
        public string NuGetConfigPath { get; }
        public ImmutableArray<string> Imports { get; set; }
        public ImmutableArray<string> DisabledDiagnostics { get; }
        public string WorkingDirectory { get; set; }
        public bool CheckOverflow { get; }
        public bool AllowUnsafe { get; }
    }
}
