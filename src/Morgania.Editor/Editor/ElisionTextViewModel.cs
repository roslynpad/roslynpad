#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// Supplies the elision-backed view model for Structured (document) views, so outlining
/// collapse has a projection seam to elide into. Hosts that need a different model export
/// a provider gated on more specific roles.
/// </summary>
[Export(typeof(ITextViewModelProvider))]
[ContentType("any")]
[TextViewRole(PredefinedTextViewRoles.Structured)]
public sealed class ElisionTextViewModelProvider : ITextViewModelProvider
{
    private readonly IProjectionBufferFactoryService _projectionBufferFactory;

    [ImportingConstructor]
    public ElisionTextViewModelProvider(IProjectionBufferFactoryService projectionBufferFactory)
    {
        _projectionBufferFactory = projectionBufferFactory;
    }

    public ITextViewModel CreateTextViewModel(ITextDataModel dataModel, ITextViewRoleSet roles)
    {
        ArgumentNullException.ThrowIfNull(dataModel);
        var snapshot = dataModel.DataBuffer.CurrentSnapshot;
        var elisionBuffer = _projectionBufferFactory.CreateElisionBuffer(
            projectionEditResolver: null,
            new NormalizedSnapshotSpanCollection(new SnapshotSpan(snapshot, 0, snapshot.Length)),
            ElisionBufferOptions.None);
        return new ElisionTextViewModel(dataModel, elisionBuffer);
    }
}

/// <summary>
/// A text view model whose visual buffer is an elision buffer over the edit buffer,
/// giving Structured (document) views the projection seam that outlining collapse
/// requires through elision/outlining via projection. With nothing elided the
/// visual buffer mirrors the edit buffer one-to-one.
/// </summary>
internal sealed class ElisionTextViewModel : ITextViewModel
{
    private readonly ITextDataModel _dataModel;
    private readonly IElisionBuffer _elisionBuffer;

    public ElisionTextViewModel(ITextDataModel dataModel, IElisionBuffer elisionBuffer)
    {
        _dataModel = dataModel;
        _elisionBuffer = elisionBuffer;
        Properties = new PropertyCollection();
    }

    public ITextDataModel DataModel => _dataModel;

    public ITextBuffer DataBuffer => _dataModel.DataBuffer;

    public ITextBuffer EditBuffer => _dataModel.DataBuffer;

    public ITextBuffer VisualBuffer => _elisionBuffer;

    public PropertyCollection Properties { get; }

    public SnapshotPoint GetNearestPointInVisualBuffer(SnapshotPoint editBufferPoint)
    {
        var visualSnapshot = (IElisionSnapshot)_elisionBuffer.CurrentSnapshot;
        var sourcePoint = editBufferPoint.TranslateTo(visualSnapshot.SourceSnapshot, PointTrackingMode.Positive);
        return visualSnapshot.MapFromSourceSnapshotToNearest(sourcePoint);
    }

    public SnapshotPoint GetNearestPointInVisualSnapshot(SnapshotPoint editBufferPoint, ITextSnapshot targetVisualSnapshot, PointTrackingMode trackingMode)
        => GetNearestPointInVisualBuffer(editBufferPoint).TranslateTo(targetVisualSnapshot, trackingMode);

    public bool IsPointInVisualBuffer(SnapshotPoint editBufferPoint, PositionAffinity affinity)
    {
        var visualSnapshot = (IElisionSnapshot)_elisionBuffer.CurrentSnapshot;
        var sourcePoint = editBufferPoint.TranslateTo(visualSnapshot.SourceSnapshot, PointTrackingMode.Positive);
        return visualSnapshot.MapFromSourceSnapshot(sourcePoint, affinity) is not null;
    }

    public void Dispose()
    {
    }
}
