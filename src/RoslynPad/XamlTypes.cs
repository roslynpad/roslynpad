using System;
using RoslynPad.Runtime;

namespace RoslynPad
{
    /// <summary>
    /// Allows referencing internal types from XAML
    /// </summary>
    public static class XamlTypes
    {
        public static readonly Type ResultObject = typeof(ResultObject);

        public static readonly Type CompilationErrorResultObject = typeof(CompilationErrorResultObject);

        public static readonly Type RestoreResultObject = typeof(RestoreResultObject);
    }
}