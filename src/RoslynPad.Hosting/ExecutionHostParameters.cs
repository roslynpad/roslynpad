using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Hosting
{
    internal class ExecutionHostParameters
    {
        public ExecutionHostParameters(
            IList<string> compileReferences,
            IList<string> runtimeReferences,
            IList<string> directReferences,
            IList<MetadataReference> frameworkReferences,
            IList<string> imports,
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
            WorkingDirectory = workingDirectory;
            OptimizationLevel = optimizationLevel;
            CheckOverflow = checkOverflow;
            AllowUnsafe = allowUnsafe;
            GlobalPackageFolder = globalPackageFolder;
        }

        public IList<string> NuGetCompileReferences { get; set; }
        public IList<string> NuGetRuntimeReferences { get; set; }
        public IList<string> DirectReferences { get; set; }
        public IList<MetadataReference> FrameworkReferences { get; set; }
        public IList<string> Imports { get; set; }
        public string WorkingDirectory { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public bool CheckOverflow { get; }
        public bool AllowUnsafe { get; }
        public string GlobalPackageFolder { get; set; }
    }
}