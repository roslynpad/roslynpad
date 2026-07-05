using System.Collections.Generic;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    public interface ICodeCleanUpFixerProvider
    {
        /// <summary>
        /// Create or return fixers which are able to handle contexts which are not
        /// represented by a content type. For example  IVsHierarchy and ItemId
        /// </summary>
        /// <returns>A set of fixers or empty</returns>
        IReadOnlyCollection<ICodeCleanUpFixer> GetFixers();

        /// <summary>
        /// Create or return fixer instances which are able to operate on the passed in content type
        /// </summary>
        /// <param name="contentType">Content type fixer can operate on</param>
        /// <returns>A set of fixers of empty</returns>
        IReadOnlyCollection<ICodeCleanUpFixer> GetFixers(IContentType contentType);
    }
}
