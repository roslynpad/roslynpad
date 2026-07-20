#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// Bridges <see cref="IStructureTag"/>s (the modern block-structure contract, produced by
/// e.g. Roslyn's structure tagger) to the <see cref="IOutliningRegionTag"/>s the outlining
/// manager aggregates: every collapsible multi-line structure tag becomes an outlining
/// region whose collapsed form and hint pass through. Morgania-authored; the VS structure
/// subsystem consuming <see cref="IStructureTag"/> was never open-sourced.
/// </summary>
[Export(typeof(ITaggerProvider))]
[ContentType("any")]
[TagType(typeof(IOutliningRegionTag))]
public sealed class StructureOutliningTaggerProvider : ITaggerProvider
{
    private readonly IBufferTagAggregatorFactoryService _aggregatorFactory;

    [ImportingConstructor]
    public StructureOutliningTaggerProvider(IBufferTagAggregatorFactoryService aggregatorFactory)
    {
        _aggregatorFactory = aggregatorFactory;
    }

    public ITagger<T>? CreateTagger<T>(ITextBuffer buffer)
        where T : ITag
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return buffer.Properties.GetOrCreateSingletonProperty(
            () => new StructureOutliningTagger(buffer, _aggregatorFactory.CreateTagAggregator<IStructureTag>(buffer))) as ITagger<T>;
    }

    internal sealed class StructureOutliningTagger : ITagger<IOutliningRegionTag>
    {
        private readonly ITagAggregator<IStructureTag> _aggregator;

        public StructureOutliningTagger(ITextBuffer buffer, ITagAggregator<IStructureTag> aggregator)
        {
            _aggregator = aggregator;
            _aggregator.TagsChanged += (_, e) =>
            {
                foreach (var span in e.Span.GetSpans(buffer))
                {
                    TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
                }
            };
        }

        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            var snapshot = spans[0].Snapshot;
            foreach (var mappingTagSpan in _aggregator.GetTags(spans))
            {
                var tag = mappingTagSpan.Tag;
                if (!tag.IsCollapsible
                    || tag.OutliningSpan is not { } outliningSpan
                    || tag.Snapshot.TextBuffer != snapshot.TextBuffer)
                {
                    continue;
                }

                var span = new SnapshotSpan(tag.Snapshot, outliningSpan).TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive);
                // Only multi-line regions collapse (the VS behavior; a single-line block
                // would fold into nothing but its own pill).
                if (snapshot.GetLineNumberFromPosition(span.Start) != snapshot.GetLineNumberFromPosition(span.End))
                {
                    yield return new TagSpan<IOutliningRegionTag>(span, new StructureOutliningRegionTag(tag));
                }
            }
        }

        private sealed class StructureOutliningRegionTag(IStructureTag structureTag) : IOutliningRegionTag
        {
            public object CollapsedForm => structureTag.GetCollapsedForm();

            public object CollapsedHintForm => structureTag.GetCollapsedHintForm();

            public bool IsDefaultCollapsed => structureTag.IsDefaultCollapsed;

            public bool IsImplementation => structureTag.IsImplementation;
        }
    }
}
