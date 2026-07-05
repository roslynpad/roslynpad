using System;

namespace Microsoft.VisualStudio.Text.PatternMatching
{
    /// <summary>
    /// Specifies flags that control optional behavior of the pattern matching.
    /// </summary>
    [Flags]
    public enum PatternMatcherCreationFlags
    {
        /// <summary>
        /// No options selected.
        /// </summary>
        None = 0,

        /// <summary>
        /// Signifies that strings differing from the initial pattern by minor spelling changes should be considered a match.
        /// </summary>
        AllowFuzzyMatching = 1,

        /// <summary>
        /// Signifies that spans indicating matched segments in candidate strings should be returned.
        /// </summary>
        IncludeMatchedSpans = 2,

        /// <summary>
        /// Signifies that a case insensitive substring match, but not a prefix should be considered a match.
        /// This covers the case of non camel case naming conventions, for example matching
        /// 'afxsettingsstore.h' when user types 'store.h'
        /// </summary>
        AllowSimpleSubstringMatching = 4
    }
}
