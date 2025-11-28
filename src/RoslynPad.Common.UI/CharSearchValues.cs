using System.Buffers;
using System.Globalization;

namespace RoslynPad.UI;

public static class CharSearchValues
{
    public static SearchValues<char> ControlChars { get; } = SearchValues.Create(GetControlChars().ToArray());

    private static IEnumerable<char> GetControlChars() =>
        Enumerable.Range(char.MinValue, char.MaxValue + 1)
        .Select(c => (char)c)
        .Where(IsControl);

    private static bool IsControl(char c) => !char.IsWhiteSpace(c) &&
        char.GetUnicodeCategory(c) switch
        {
            UnicodeCategory.Control or
            UnicodeCategory.Format or
            UnicodeCategory.OtherNotAssigned => true,
            _ => false,
        };
}
