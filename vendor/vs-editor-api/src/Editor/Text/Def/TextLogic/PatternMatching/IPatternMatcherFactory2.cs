namespace Microsoft.VisualStudio.Text.PatternMatching
{
    using Microsoft.VisualStudio.Text.PatternMatching;

    /// <summary>
    /// Provides instances of a <see cref="IPatternMatcher"/> for a given
    /// search string and creation options.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IPatternMatcherFactory2 factory = null;
    /// </remarks>
    public interface IPatternMatcherFactory2 : IPatternMatcherFactory
    {
        /// <summary>
        /// Gets an <see cref="IPatternMatcher"/> given a search pattern and search options.
        /// </summary>
        /// <param name="pattern">Describes the search pattern that candidate strings will be compared against for relevancy.</param>
        /// <param name="creationOptions">Defines parameters for what should be considered relevant in a match.</param>
        /// <param name="linkedMatcher">A matcher whose cache should be shared with the created matcher.</param>
        /// <remarks>
        /// <para>
        /// As opposed to <see cref="IPatternMatcherFactory.CreatePatternMatcher(string, PatternMatcherCreationOptions)"/>, this overload
        /// creates a <see cref="IPatternMatcher"/> with a shared cache. Use this overload in contexts with frequently changing <paramref name="pattern"/>s
        /// to reduce allocations and throw-away work. Note that sharing the cache between <see cref="IPatternMatcher"/>s used from multiple
        /// threads may lead to lock contention. It's recommended to profile prior to opting in.
        /// </para>
        /// <para>
        /// This pattern matcher uses the concepts of a 'Pattern' and a 'Candidate' to to differentiate between what the user types to search
        /// and what the system compares against. The pattern and some <see cref="PatternMatcherCreationOptions"/> are specified in here in order to obtain an <see cref="IPatternMatcher"/>.
        /// 
        /// The user can then call <see cref="IPatternMatcher.TryMatch(string)"/> repeatedly with multiple candidates to filter out non-matches, and obtain sortable <see cref="PatternMatch"/> objects to help decide
        /// what the user actually wanted.
        /// 
        /// A few examples are useful here. Suppose the user obtains an IPatternMatcher using the following:
        /// Pattern = "PatMat"
        ///
        /// The following calls to TryMatch could expect these results:
        /// Candidate = "PatternMatcher"
        /// Returns a match containing <see cref="PatternMatchKind.CamelCaseExact"/>.
        ///
        /// Candidate = "IPatternMatcher"
        /// Returns a match containing <see cref="PatternMatchKind.CamelCaseSubstring"/>
        ///
        /// Candidate = "patmat"
        /// Returns a match containing <see cref="PatternMatchKind.Exact"/>, but <see cref="PatternMatch.IsCaseSensitive"/> will be false.
        ///
        /// Candidate = "Not A Match"
        /// Returns <see langword="null"/>.
        ///
        /// To determine sort order, call <see cref="PatternMatch.CompareTo(PatternMatch)"/>.
        /// </para>
        /// </remarks>
        IPatternMatcher CreatePatternMatcher(string pattern, PatternMatcherCreationOptions creationOptions, IPatternMatcher linkedMatcher);
    }
}
