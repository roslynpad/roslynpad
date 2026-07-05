namespace Microsoft.VisualStudio.Text.PatternMatching
{
    /// <summary>
    /// Provides instances of a <see cref="IPatternMatcher"/> for a given
    /// search string and creation options.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IPatternMatcherFactory factory = null;
    /// </remarks>
    public interface IPatternMatcherFactory
    {
        /// <summary>
        /// Gets an <see cref="IPatternMatcher"/> given a search pattern and search options.
        /// </summary>
        /// <param name="pattern">Describes the search pattern that candidate strings will be compared against for relevancy.</param>
        /// <param name="creationOptions">Defines parameters for what should be considered relevant in a match.</param>
        /// <remarks>
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
        /// </remarks>
        IPatternMatcher CreatePatternMatcher(string pattern, PatternMatcherCreationOptions creationOptions);
    }
}
