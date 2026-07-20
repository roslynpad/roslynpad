// Avalonia replacement for the excluded vendor file Workspaces/WpfTextBufferVisibilityTracker.cs.
// WPF's UIElement.IsVisible flips with effective visibility (an element leaving/entering the
// rendered tree); the Avalonia equivalent is visual-tree attachment — the dock detaches inactive
// document views — combined with the IsVisible flag chain for elements hidden in place.

using System;
using System.ComponentModel.Composition;
using Avalonia;
using Avalonia.Controls;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Workspaces;

[Export(typeof(ITextBufferVisibilityTracker))]
internal sealed class AvaloniaTextBufferVisibilityTracker
    : AbstractTextBufferVisibilityTracker<IWpfTextView, EventHandler<VisualTreeAttachmentEventArgs>>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public AvaloniaTextBufferVisibilityTracker(
        ITextBufferAssociatedViewService associatedViewService,
        IThreadingContext threadingContext)
        : base(associatedViewService, threadingContext)
    {
    }

    protected override bool IsVisible(IWpfTextView view)
        => TopLevel.GetTopLevel(view.VisualElement) is not null && view.VisualElement.IsEffectivelyVisible;

    protected override EventHandler<VisualTreeAttachmentEventArgs> GetVisiblityChangeCallback(VisibleTrackerData visibleTrackerData)
        => (sender, args) => visibleTrackerData.TriggerCallbacks();

    protected override void AddVisibilityChangedCallback(IWpfTextView view, EventHandler<VisualTreeAttachmentEventArgs> visibilityChangedCallback)
    {
        view.VisualElement.AttachedToVisualTree += visibilityChangedCallback;
        view.VisualElement.DetachedFromVisualTree += visibilityChangedCallback;
    }

    protected override void RemoveVisibilityChangedCallback(IWpfTextView view, EventHandler<VisualTreeAttachmentEventArgs> visibilityChangedCallback)
    {
        view.VisualElement.AttachedToVisualTree -= visibilityChangedCallback;
        view.VisualElement.DetachedFromVisualTree -= visibilityChangedCallback;
    }
}
