using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.VisualStudio.Text.PatternMatching
{
    /// <summary>
    /// Defines context for what should be considered relevant in a pattern match.
    /// </summary>
    public sealed class PatternMatcherCreationOptions
    {
        /// <summary>
        /// Used to tailor character comparisons to the correct culture.
        /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
        [SuppressMessage("Microsoft.Security", "CA2104", Justification = "CultureInfo is immutable")]
        public readonly CultureInfo CultureInfo;
#pragma warning restore CA1051 // Do not declare visible instance fields

        /// <summary>
        /// A set of biniary options, used to control options like case-sensitivity.
        /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
        public readonly PatternMatcherCreationFlags Flags;
#pragma warning disable CA1051 // Do not declare visible instance fields

        /// <summary>
        /// Characters that should be considered as describing a container/contained boundary. When matching types, this can be the '.' character
        /// e.g. Namespace.Class.Property, so that the search can tailor behavior to better match Property first, then Class, then Namespace.
        /// This also can work with directory separators in filenames and any other logical container/contained pattern in candidate strings.
        ///
        /// <see langword="null"/> signifies no characters are container boundaries.
        /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
        [SuppressMessage("Microsoft.Security", "CA2104", Justification = "Cannot make a breaking change")]
        public readonly IReadOnlyCollection<char> ContainerSplitCharacters;
#pragma warning restore CA1051 // Do not declare visible instance fields

        /// <summary>
        /// Creates an instance of <see cref="PatternMatcherCreationOptions"/>.
        /// </summary>
        /// <param name="cultureInfo">Used to tailor character comparisons to the correct culture.</param>
        /// <param name="flags">A set of biniary options, used to control options like case-sensitivity.</param>
        /// <param name="containerSplitCharacters">
        /// Characters that should be considered as describing a container/contained boundary. When matching types, this can be the '.' character
        /// e.g. Namespace.Class.Property, so that the search can tailor behavior to better match Property first, then Class, then Namespace.
        /// This also can work with directory separators in filenames and any other logical container/contained pattern in candidate strings.
        ///
        /// <see langword="null"/> signifies no characters are container boundaries.
        /// </param>
        public PatternMatcherCreationOptions(CultureInfo cultureInfo, PatternMatcherCreationFlags flags, IReadOnlyCollection<char> containerSplitCharacters = null)
        {
            CultureInfo = cultureInfo;
            Flags = flags;
            ContainerSplitCharacters = containerSplitCharacters;
        }
    }
}
