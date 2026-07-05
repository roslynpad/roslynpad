// A representative Visual Studio editor extension, written the way the canonical VSSDK
// samples are (a text adornment on its own layer, a view tagger, a margin), recompiled
// against Morgania's contract assemblies. The complete mechanical diff from source
// written for real VS:
//
//   1. Microsoft.VisualStudio.* namespaces -> Morgania.*;
//   2. WPF visual types -> Avalonia equivalents: FrameworkElement -> Control,
//      Brush -> IBrush, System.Windows.Shapes -> Avalonia.Controls.Shapes;
//   3. MEF v1 -> System.Composition v2 (ADR-003): field exports become properties,
//      System.ComponentModel.Composition -> System.Composition.
//
// Interfaces, members, attribute names, and registration patterns are the VS ones,
// untouched.
namespace TodoExtension
{
    using System;
    using System.Collections.Generic;
    using System.Composition;

    using Avalonia.Controls;
    using Avalonia.Controls.Shapes;
    using Avalonia.Media;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Tags every "TODO" with a text marker (the classic todo-tagger sample shape).
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(TextMarkerTag))]
    public sealed class TodoTaggerProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer)
            where T : ITag
        {
            return new TodoTagger() as ITagger<T>;
        }

        private sealed class TodoTagger : ITagger<TextMarkerTag>
        {
#pragma warning disable CS0067
            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore CS0067

            public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                foreach (var span in spans)
                {
                    string text = span.GetText();
                    for (int index = text.IndexOf("TODO", StringComparison.Ordinal);
                         index >= 0;
                         index = text.IndexOf("TODO", index + 4, StringComparison.Ordinal))
                    {
                        yield return new TagSpan<TextMarkerTag>(
                            new SnapshotSpan(span.Snapshot, span.Start + index, 4),
                            new TextMarkerTag("todo"));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Highlights every "TODO" on the extension's own adornment layer (the TextAdornment
    /// sample shape: a creation listener exporting an AdornmentLayerDefinition and
    /// repopulating on layout).
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public sealed class TodoAdornmentTextViewCreationListener : IWpfTextViewCreationListener
    {
        public const string LayerName = "TodoAdornment";

        [Export]
        [Name(LayerName)]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        public AdornmentLayerDefinition editorAdornmentLayer { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            textView.LayoutChanged += (sender, e) => OnLayoutChanged(textView, e);
        }

        private static void OnLayoutChanged(IWpfTextView view, TextViewLayoutChangedEventArgs e)
        {
            var layer = view.GetAdornmentLayer(LayerName);
            foreach (var line in e.NewOrReformattedLines)
            {
                string text = line.Extent.GetText();
                for (int index = text.IndexOf("TODO", StringComparison.Ordinal);
                     index >= 0;
                     index = text.IndexOf("TODO", index + 4, StringComparison.Ordinal))
                {
                    var span = new SnapshotSpan(view.TextSnapshot, line.Start + index, 4);
                    var geometry = view.TextViewLines.GetTextMarkerGeometry(span);
                    if (geometry != null)
                    {
                        var highlight = new Path
                        {
                            Data = geometry,
                            Fill = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xD7, 0x00)),
                            Stroke = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xD7, 0x00)),
                            StrokeThickness = 0.5,
                        };
                        Canvas.SetLeft(highlight, -view.ViewportLeft);
                        Canvas.SetTop(highlight, -view.ViewportTop);
                        layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, span, null, highlight, null);
                    }
                }
            }
        }
    }

    /// <summary>
    /// A bottom-container info margin (the margin sample shape).
    /// </summary>
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(TodoMargin.MarginName)]
    [MarginContainer(PredefinedMarginNames.Bottom)]
    [ContentType("text")]
    [Order(After = PredefinedMarginNames.HorizontalScrollBar)]
    public sealed class TodoMarginProvider : IWpfTextViewMarginProvider
    {
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            return new TodoMargin();
        }
    }

    internal sealed class TodoMargin : Border, IWpfTextViewMargin
    {
        public const string MarginName = "TodoMargin";

        public TodoMargin()
        {
            Height = 20.0;
            Child = new TextBlock { Text = "TODO extension margin", FontSize = 11.0 };
        }

        public Control VisualElement
        {
            get { return this; }
        }

        public double MarginSize
        {
            get { return Height; }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Equals(marginName, MarginName, StringComparison.OrdinalIgnoreCase) ? this : null;
        }

        public void Dispose()
        {
        }
    }
}
