namespace Microsoft.VisualStudio.Demo;

using System.Globalization;
using System.Text;

/// <summary>
/// Builds the sample document: C#-flavored code, repeated to a size that makes scrolling and
/// viewport-only formatting observable.
/// </summary>
public static class SampleDocument
{
    private const string Block = """
        // Morgania demo — the VS editor core rendering on Avalonia.
        // Scroll with the mouse wheel; only visible lines are ever formatted.
        // Ctrl+M folds the { } block at the caret (elision projection, M5);
        // Ctrl+Wheel zooms; Alt+Z toggles word wrap; type to edit (Alt+Click adds a caret).
        // Click any word and every occurrence lights up (text search + word navigator);
        // Ctrl+Z / Ctrl+Y undo and redo; Ctrl+C/X/V clipboard; Ctrl+arrows jump by word.
        // The "---- block ----" separators get extra spacing from a line transform (M5),
        // and the blue status bar below is a custom bottom margin with live caret info.
        // completion (M6): Ctrl+Space completes words (arrows navigate, Tab/Enter commit,
        // Esc dismisses), hovering a word shows Quick Info, and typing "(" after a name
        // brings up signature help (Up/Down cycles the overloads).
        namespace Microsoft.VisualStudio.Demo;

        public sealed class Greeter
        {
            private readonly string _name;
            private const int MeaningOfLife = 42;

            public Greeter(string name)
            {
                _name = name; // remembered for later
            }

            public string Greet(double enthusiasm)
            {
                var greeting = "Hello, " + _name + "!";
                var accent = "#569CD6";     // intra-text color swatches (M3)
                var background = "#1E1E1E"; // negotiated space in the line
                return enthusiasm > 0.5 ? greeting.ToUpperInvariant() : greeting;
            }
        }

        """;

    public static string Text { get; } = Build();

    private static string Build()
    {
        var builder = new StringBuilder();
        for (int i = 0; i < 40; i++)
        {
            builder.Append("// ---- block ").Append(i.ToString(CultureInfo.InvariantCulture)).AppendLine(" ----");
            builder.AppendLine(Block);
        }

        return builder.ToString();
    }
}
