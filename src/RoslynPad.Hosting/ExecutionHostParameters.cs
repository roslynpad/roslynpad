using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Hosting
{
    internal class ExecutionHostParameters
    {
        public ExecutionHostParameters(IList<string> compileReferences, IList<string> runtimeReferences, IList<MetadataReference> frameworkReferences, IList<string> imports, string workingDirectory, string globalPackageFolder, bool shadowCopyAssemblies = true, OptimizationLevel optimizationLevel = OptimizationLevel.Debug, bool checkOverflow = false, bool allowUnsafe = true)
        {
            CompileReferences = compileReferences;
            RuntimeReferences = runtimeReferences;
            FrameworkReferences = frameworkReferences;
            Imports = imports;
            WorkingDirectory = workingDirectory;
            ShadowCopyAssemblies = shadowCopyAssemblies;
            OptimizationLevel = optimizationLevel;
            CheckOverflow = checkOverflow;
            AllowUnsafe = allowUnsafe;
            GlobalPackageFolder = globalPackageFolder;
        }

        public IList<string> CompileReferences { get; set; }
        public IList<string> RuntimeReferences { get; set; }
        public IList<MetadataReference> FrameworkReferences { get; set; }
        public IList<string> Imports { get; set; }
        public string WorkingDirectory { get; }
        public bool ShadowCopyAssemblies { get; }
        public OptimizationLevel OptimizationLevel { get; }
        public bool CheckOverflow { get; }
        public bool AllowUnsafe { get; }
        public string GlobalPackageFolder { get; set; }
    }
}