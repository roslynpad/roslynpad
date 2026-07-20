#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Composition;

using Avalonia.Controls;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// Bridges <see cref="IntraTextAdornmentTag"/>s (and their cross-platform base) to the
/// space-negotiation pipeline: each tag becomes a
/// <see cref="SpaceNegotiatingAdornmentTag"/> sized from the adornment control, and the
/// control itself is positioned over the reserved space after every layout. Morgania-
/// authored; the VS IntraTextAdornmentSupport implementation was never open-sourced.
/// </summary>
[Export(typeof(IViewTaggerProvider))]
[ContentType("text")]
[TagType(typeof(SpaceNegotiatingAdornmentTag))]
public sealed class IntraTextAdornmentSupportProvider : IViewTaggerProvider
{
    private readonly IViewTagAggregatorFactoryService _tagAggregatorFactory;

    [ImportingConstructor]
    public IntraTextAdornmentSupportProvider(IViewTagAggregatorFactoryService tagAggregatorFactory)
    {
        _tagAggregatorFactory = tagAggregatorFactory;
    }

    public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer)
        where T : ITag
    {
        ArgumentNullException.ThrowIfNull(textView);
        ArgumentNullException.ThrowIfNull(buffer);
        if (textView is not WpfTextView view || buffer != textView.TextBuffer)
        {
            return null;
        }

        return textView.Properties.GetOrCreateSingletonProperty(
            () => new IntraTextAdornmentSupport(view, _tagAggregatorFactory.CreateTagAggregator<XPlatIntraTextAdornmentTag>(view))) as ITagger<T>;
    }

    internal sealed class IntraTextAdornmentSupport : ITagger<SpaceNegotiatingAdornmentTag>
    {
        internal const string LayerName = "Intra Text Adornment";

        private readonly WpfTextView _view;
        private readonly ITagAggregator<XPlatIntraTextAdornmentTag> _tagAggregator;
        private readonly Dictionary<XPlatIntraTextAdornmentTag, Control> _visibleAdornments = [];

        public IntraTextAdornmentSupport(WpfTextView view, ITagAggregator<XPlatIntraTextAdornmentTag> tagAggregator)
        {
            _view = view;
            _tagAggregator = tagAggregator;
            _tagAggregator.TagsChanged += (_, e) =>
            {
                foreach (var span in e.Span.GetSpans(_view.TextBuffer))
                {
                    TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
                }
            };
            _view.LayoutChanged += (_, _) => PositionAdornments();
        }

        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

        public IEnumerable<ITagSpan<SpaceNegotiatingAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var querySpan in spans)
            {
                foreach (var mappingTagSpan in _tagAggregator.GetTags(querySpan))
                {
                    var tag = mappingTagSpan.Tag;
                    if (tag.Adornment is not Control control)
                    {
                        continue;
                    }

                    foreach (var span in mappingTagSpan.Span.GetSpans(querySpan.Snapshot))
                    {
                        // The negotiated space comes from the tag's explicit metrics, falling
                        // back to the measured control.
                        control.Measure(Avalonia.Size.Infinity);
                        double width = control.DesiredSize.Width;
                        double textHeight = tag.TextHeight ?? control.DesiredSize.Height;
                        double baseline = tag.Baseline ?? textHeight;
                        yield return new TagSpan<SpaceNegotiatingAdornmentTag>(
                            span,
                            new SpaceNegotiatingAdornmentTag(
                                width,
                                tag.TopSpace ?? 0.0,
                                baseline,
                                textHeight,
                                tag.BottomSpace ?? 0.0,
                                tag.Affinity ?? PositionAffinity.Predecessor,
                                identityTag: tag,
                                providerTag: this));
                    }
                }
            }
        }

        /// <summary>Places each visible intra-text adornment control over its reserved space.</summary>
        private void PositionAdornments()
        {
            if (_view.IsClosed || !_view.TryGetTextViewLines(out var textViewLines))
            {
                return;
            }

            var layer = _view.GetAdornmentLayer(LayerName);
            var stillVisible = new HashSet<XPlatIntraTextAdornmentTag>();
            foreach (var line in textViewLines)
            {
                foreach (object identity in line.GetAdornmentTags(this))
                {
                    if (identity is not XPlatIntraTextAdornmentTag tag || tag.Adornment is not Control control)
                    {
                        continue;
                    }

                    var bounds = line.GetAdornmentBounds(tag);
                    if (bounds is not { } adornmentBounds)
                    {
                        continue;
                    }

                    stillVisible.Add(tag);
                    if (_visibleAdornments.TryAdd(tag, control))
                    {
                        layer.AddAdornment(
                            AdornmentPositioningBehavior.OwnerControlled,
                            null,
                            tag,
                            control,
                            (removedTag, _) => _visibleAdornments.Remove((XPlatIntraTextAdornmentTag)removedTag!));
                    }

                    // The adornment's baseline sits on the line's text baseline (the tag's
                    // default baseline is its height, i.e. bottom-on-baseline).
                    double textHeight = tag.TextHeight ?? control.DesiredSize.Height;
                    double baseline = tag.Baseline ?? textHeight;
                    Canvas.SetLeft(control, adornmentBounds.Left - _view.ViewportLeft);
                    Canvas.SetTop(control, line.TextTop + line.Baseline - baseline - _view.ViewportTop);
                }
            }

            foreach (var tag in _visibleAdornments.Keys.Where(tag => !stillVisible.Contains(tag)).ToList())
            {
                layer.RemoveAdornmentsByTag(tag);
                tag.RemovalCallback?.Invoke(tag, tag.Adornment);
            }
        }
    }
}
