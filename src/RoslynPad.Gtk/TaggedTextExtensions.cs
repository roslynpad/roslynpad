using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Cairo;
using Microsoft.CodeAnalysis;
using Mono.TextEditor.Highlighting;

namespace RoslynPad.Gtk
{
    public static class TaggedTextExtensions
    {
        public static string ToPangoMarkup(this IEnumerable<TaggedText> parts, bool bold = false)
        {
            var text = string.Concat(parts.Select(Format));
            return bold ? $"<b>{text}</b>" : text;
        }

        private static string Format(TaggedText taggedText)
        {
            string colorString = null;
            var colorStyle = SyntaxModeService.GetColorStyle(MonoDevelop.Ide.IdeApp.Preferences.ColorScheme);
            string color;
            if (RoslynSemanticHighlighting.ClassificationMap.TryGetValue(taggedText.Tag, out color))
            {
                var foreground = colorStyle.GetChunkStyle(color).Foreground;
                colorString = GetColorString(foreground);
            }

            var escapedText = Escape(taggedText.Text);

            return colorString != null
                ? "<span foreground=\"" + colorString + "\">" + escapedText + "</span>"
                : escapedText;
        }

        private static string Escape(string text)
        {
            return new XText(text).ToString();
        }

        private static string GetColorString(Color color)
        {
            return $"#{ (int)(color.R * 256):X02}{ (int)(color.G * 256):X02}{ (int)(color.B * 256):X02}";
        }
    }
}