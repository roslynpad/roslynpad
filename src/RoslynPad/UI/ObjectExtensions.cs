using System.Runtime.CompilerServices;

namespace RoslynPad.UI;

public static class ObjectExtensions
{
    public static T NotNull<T>(this T? value, [CallerArgumentExpression(nameof(value))] string expression = "") =>
        value ?? throw new InvalidOperationException("Expression not expected to be null: " + expression);

    // In this namespace so it beats the ambiguous imported candidates
    // (NuGet.Packaging.CollectionExtensions vs. Roslyn's internal ICollectionExtensions,
    // the latter visible through the EditorFeatures InternalsVisibleTo grant).
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
