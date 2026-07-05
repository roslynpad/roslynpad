using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    /// <summary>
    /// Context to indicate to code clean up fixers what they need to fix.
    /// The concrete implementations will have the specific context such as a file, or IVSHierarchy available
    /// </summary>
    public interface ICodeCleanUpScope
    {
    }
}
