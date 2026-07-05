using System;
using System.Composition;
using System.Linq;
using static Microsoft.VisualStudio.Text.PatternMatching.PatternMatcherCreationFlags;

namespace Microsoft.VisualStudio.Text.PatternMatching.Implementation
{
    [Export(typeof(IPatternMatcherFactory))]
    [Shared]
    public class PatternMatcherFactory : IPatternMatcherFactory2
    {
        public IPatternMatcher CreatePatternMatcher(string pattern, PatternMatcherCreationOptions creationOptions)
        {
            return this.CreatePatternMatcher(pattern, creationOptions, linkedMatcher: null);
        }

#pragma warning disable CA1822
        public IPatternMatcher CreatePatternMatcher(string pattern, PatternMatcherCreationOptions creationOptions, IPatternMatcher linkedMatcher)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                throw new ArgumentException("A non-empty pattern is required to create a pattern matcher", nameof(pattern));
            }

            if (creationOptions == null)
            {
                throw new ArgumentNullException(nameof(creationOptions));
            }

            var matcher = linkedMatcher as PatternMatcher;

            if (creationOptions.ContainerSplitCharacters == null)
            {
                return PatternMatcher.CreateSimplePatternMatcher(
                    pattern,
                    creationOptions.CultureInfo,
                    creationOptions.Flags.HasFlag(IncludeMatchedSpans),
                    creationOptions.Flags.HasFlag(AllowFuzzyMatching),
                    creationOptions.Flags.HasFlag(AllowSimpleSubstringMatching),
                    matcher);
            }
            else
            {
                return PatternMatcher.CreateContainerPatternMatcher(
                    pattern.Split(creationOptions.ContainerSplitCharacters.ToArray()),
                    creationOptions.ContainerSplitCharacters,
                    creationOptions.CultureInfo,
                    creationOptions.Flags.HasFlag(AllowFuzzyMatching),
                    creationOptions.Flags.HasFlag(AllowSimpleSubstringMatching),
                    creationOptions.Flags.HasFlag(IncludeMatchedSpans),
                    matcher);
            }
        }
#pragma warning restore CA1822
    }
}
