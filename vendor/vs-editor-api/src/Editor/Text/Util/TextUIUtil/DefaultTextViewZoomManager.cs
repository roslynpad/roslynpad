using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Utilities
{
    [System.Composition.Shared]
    [ExportImplementation(typeof(ITextViewZoomManager))]
    [Name("default")]
    public class DefaultTextViewZoomManager : ITextViewZoomManager
    {
        public void ZoomIn(ITextView textView)
        {
        }

        public double ZoomLevel(ITextView textView)
        {
            return 100;
        }

        public void ZoomOut(ITextView textView)
        {
        }

        public void ZoomTo(ITextView textView, double zoomLevel)
        {
        }
    }
}
