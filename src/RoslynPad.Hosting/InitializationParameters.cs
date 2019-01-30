using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Hosting
{
    [DataContract]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
    internal class InitializationParameters
    {
        public InitializationParameters(IList<string> compileReferences, IList<string> runtimeReferences, IList<string> imports, string workingDirectory, bool shadowCopyAssemblies = true, OptimizationLevel optimizationLevel = OptimizationLevel.Debug, bool checkOverflow = false, bool allowUnsafe = true)
        {
            CompileReferences = compileReferences;
            RuntimeReferences = runtimeReferences;
            Imports = imports;
            WorkingDirectory = workingDirectory;
            ShadowCopyAssemblies = shadowCopyAssemblies;
            OptimizationLevel = optimizationLevel;
            CheckOverflow = checkOverflow;
            AllowUnsafe = allowUnsafe;
        }

        [DataMember]
        public IList<string> CompileReferences { get; set; }
        [DataMember]
        public IList<string> RuntimeReferences { get; set; }
        [DataMember]
        public IList<string> Imports { get; set; }
        [DataMember]
        public string WorkingDirectory { get; private set; }
        [DataMember]
        public bool ShadowCopyAssemblies { get; private set; }
        [DataMember]
        public OptimizationLevel OptimizationLevel { get; private set; }
        [DataMember]
        public bool CheckOverflow { get; private set; }
        [DataMember]
        public bool AllowUnsafe { get; private set; }
    }
}