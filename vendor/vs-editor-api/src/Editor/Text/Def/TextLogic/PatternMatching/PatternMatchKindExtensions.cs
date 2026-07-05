namespace Microsoft.VisualStudio.Text.PatternMatching
{
    public static class PatternMatchKindExtensions
    {
        /// <summary>
        /// Compares two <see cref="PatternMatchKind"/> values, suggesting which one is more likely to be what the user was searching for.
        /// </summary>
        /// <param name="kind1">Item to be compared.</param>
        /// <param name="kind2">Item to be compared.</param>
        /// <returns>A negative value means kind1 is preferable, positive means kind2 is preferable. Zero means they are equivalent.</returns>
        public static int CompareTo(this PatternMatchKind kind1, PatternMatchKind kind2)
        {
            return kind1 - kind2;
        }
    }
}
