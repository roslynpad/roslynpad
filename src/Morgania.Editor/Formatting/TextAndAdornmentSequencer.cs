#nullable enable

namespace Microsoft.VisualStudio.Text.Formatting.Implementation;

using System.Collections;
using System.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;

/// <summary>
/// Sequences a line's text and its space-negotiating adornments into the element stream the
/// formatter consumes. Morgania-authored: the VS sequencer implementation was
/// never open-sourced. Elements are produced from the view's
/// <see cref="SpaceNegotiatingAdornmentTag"/> aggregation; adornments that replace a
/// non-empty span suppress its text.
/// </summary>
[Export(typeof(ITextAndAdornmentSequencerFactoryService))]
[Shared]
public sealed class TextAndAdornmentSequencerFactoryService : ITextAndAdornmentSequencerFactoryService
{
    private readonly IViewTagAggregatorFactoryService _tagAggregatorFactory;

    [ImportingConstructor]
    public TextAndAdornmentSequencerFactoryService(IViewTagAggregatorFactoryService tagAggregatorFactory)
    {
        _tagAggregatorFactory = tagAggregatorFactory;
    }

    public ITextAndAdornmentSequencer Create(ITextView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return view.Properties.GetOrCreateSingletonProperty(
            () => new TextAndAdornmentSequencer(view, _tagAggregatorFactory.CreateTagAggregator<SpaceNegotiatingAdornmentTag>(view)));
    }

    internal sealed class TextAndAdornmentSequencer : ITextAndAdornmentSequencer
    {
        private readonly ITextView _view;
        private readonly ITagAggregator<SpaceNegotiatingAdornmentTag> _tagAggregator;

        public TextAndAdornmentSequencer(ITextView view, ITagAggregator<SpaceNegotiatingAdornmentTag> tagAggregator)
        {
            _view = view;
            _tagAggregator = tagAggregator;
            _tagAggregator.TagsChanged += (_, e) => SequenceChanged?.Invoke(this, new TextAndAdornmentSequenceChangedEventArgs(e.Span));
        }

        public event EventHandler<TextAndAdornmentSequenceChangedEventArgs>? SequenceChanged;

        public IBufferGraph BufferGraph => _view.BufferGraph;

        public ITextBuffer TopBuffer => _view.TextViewModel.VisualBuffer;

        public ITextBuffer SourceBuffer => _view.TextViewModel.EditBuffer;

        public ITextAndAdornmentCollection CreateTextAndAdornmentCollection(ITextSnapshotLine topLine, ITextSnapshot sourceTextSnapshot)
        {
            ArgumentNullException.ThrowIfNull(topLine);
            return CreateTextAndAdornmentCollection(topLine.ExtentIncludingLineBreak, sourceTextSnapshot);
        }

        public ITextAndAdornmentCollection CreateTextAndAdornmentCollection(SnapshotSpan topSpan, ITextSnapshot sourceTextSnapshot)
        {
            ArgumentNullException.ThrowIfNull(sourceTextSnapshot);

            // The top span maps down to one source span per visible segment (elision hides
            // the text between them); identity view models map to exactly one.
            var elements = new List<ISequenceElement>();
            foreach (var sourceSpan in BufferGraph.MapDownToSnapshot(topSpan, SpanTrackingMode.EdgeInclusive, sourceTextSnapshot))
            {
                AppendElements(elements, sourceSpan, sourceTextSnapshot);
            }

            if (elements.Count == 0)
            {
                // An empty (or fully elided) line still needs one empty text element to
                // keep the formatter fed.
                int start = BufferGraph.MapDownToSnapshot(topSpan.Start, PointTrackingMode.Negative, sourceTextSnapshot, PositionAffinity.Successor)?.Position ?? 0;
                elements.Add(new TextSequenceElement(MapSpan(sourceTextSnapshot, start, start)));
            }

            return new TextAndAdornmentCollection(this, elements);
        }

        private void AppendElements(List<ISequenceElement> elements, SnapshotSpan sourceSpan, ITextSnapshot sourceTextSnapshot)
        {
            // Collect adornments intersecting the span, in buffer order.
            var adornments = new List<(SnapshotSpan Span, SpaceNegotiatingAdornmentTag Tag)>();
            foreach (var mappingTagSpan in _tagAggregator.GetTags(sourceSpan))
            {
                foreach (var span in mappingTagSpan.Span.GetSpans(sourceTextSnapshot))
                {
                    if (span.Start >= sourceSpan.Start && span.End <= sourceSpan.End)
                    {
                        adornments.Add((span, mappingTagSpan.Tag));
                    }
                }
            }

            adornments.Sort(static (left, right) => left.Span.Start.Position.CompareTo(right.Span.Start.Position));

            int current = sourceSpan.Start.Position;
            foreach (var (span, tag) in adornments)
            {
                if (span.Start.Position < current)
                {
                    continue; // Overlapping adornments: first wins.
                }

                if (span.Start.Position > current)
                {
                    elements.Add(new TextSequenceElement(MapSpan(sourceTextSnapshot, current, span.Start.Position)));
                }

                elements.Add(new AdornmentSequenceElement(MapSpan(sourceTextSnapshot, span.Start.Position, span.End.Position), tag));
                current = span.End.Position;
            }

            if (current < sourceSpan.End.Position)
            {
                elements.Add(new TextSequenceElement(MapSpan(sourceTextSnapshot, current, sourceSpan.End.Position)));
            }
        }

        private IMappingSpan MapSpan(ITextSnapshot snapshot, int start, int end)
            => BufferGraph.CreateMappingSpan(new SnapshotSpan(snapshot, start, end - start), SpanTrackingMode.EdgeExclusive);
    }

    private sealed class TextSequenceElement : ISequenceElement
    {
        public TextSequenceElement(IMappingSpan span) => Span = span;

        public IMappingSpan Span { get; }

        public bool ShouldRenderText => true;
    }

    internal sealed class AdornmentSequenceElement : IAdornmentElement
    {
        private readonly SpaceNegotiatingAdornmentTag _tag;

        public AdornmentSequenceElement(IMappingSpan span, SpaceNegotiatingAdornmentTag tag)
        {
            Span = span;
            _tag = tag;
        }

        public IMappingSpan Span { get; }

        public bool ShouldRenderText => false;

        public double Width => _tag.Width;

        public double TopSpace => _tag.TopSpace;

        public double Baseline => _tag.Baseline;

        public double TextHeight => _tag.TextHeight;

        public double BottomSpace => _tag.BottomSpace;

        public object IdentityTag => _tag.IdentityTag;

        public object ProviderTag => _tag.ProviderTag;

        public PositionAffinity Affinity => _tag.Affinity;
    }

    private sealed class TextAndAdornmentCollection : ITextAndAdornmentCollection
    {
        private readonly List<ISequenceElement> _elements;

        public TextAndAdornmentCollection(ITextAndAdornmentSequencer sequencer, List<ISequenceElement> elements)
        {
            Sequencer = sequencer;
            _elements = elements;
        }

        public ITextAndAdornmentSequencer Sequencer { get; }

        public ISequenceElement this[int index]
        {
            get => _elements[index];
            set => throw new NotSupportedException();
        }

        public int Count => _elements.Count;

        public bool IsReadOnly => true;

        public int IndexOf(ISequenceElement item) => _elements.IndexOf(item);

        public bool Contains(ISequenceElement item) => _elements.Contains(item);

        public void CopyTo(ISequenceElement[] array, int arrayIndex) => _elements.CopyTo(array, arrayIndex);

        public IEnumerator<ISequenceElement> GetEnumerator() => _elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(ISequenceElement item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public void Insert(int index, ISequenceElement item) => throw new NotSupportedException();

        public bool Remove(ISequenceElement item) => throw new NotSupportedException();

        public void RemoveAt(int index) => throw new NotSupportedException();
    }
}
