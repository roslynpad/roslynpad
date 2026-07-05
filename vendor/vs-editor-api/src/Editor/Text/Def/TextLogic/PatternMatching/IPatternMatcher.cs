using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.PatternMatching
{
    /// <summary>
    /// Defines a pattern matcher that can compare a candidate string against a search pattern to identify relevance. <see cref="IPatternMatcherFactory"/> defines
    /// the way to obtain an <see cref="IPatternMatcher"/> given a search pattern and options.
    /// </summary>
    public interface IPatternMatcher
    {
        /// <summary>
        /// Determines if, and how well a candidate string matches a search pattern and a set of <see cref="PatternMatcherCreationOptions"/>.
        /// </summary>
        /// <param name="candidate">The string to evaluate for relevancy.</param>
        /// <returns>A <see cref="PatternMatch"/> object describing how well the candidate matched the pattern. If no match is found, this returns <see langword="null"/> instead.</returns>
        /// <remarks>
        /// This pattern matcher uses the concepts of a 'Pattern' and a 'Candidate' to to differentiate between what the user types to search
        /// and what the system compares against. The pattern and some <see cref="PatternMatcherCreationOptions"/> are specified in <see cref="IPatternMatcherFactory"/> in order to obtain an <see cref="IPatternMatcher"/>.
        /// 
        /// The user can then call this method repeatedly with multiple candidates to filter out non-matches, and obtain sortable <see cref="PatternMatch"/> objects to help decide
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
        PatternMatch? TryMatch(string candidate);

        /// <summary>
        /// Determines whether given pattern is invalid,
        /// in which case <see cref="TryMatch(string)"/> would return no <see cref="PatternMatch"/>es.
        /// </summary>
        bool HasInvalidPattern { get; }
    }
}
