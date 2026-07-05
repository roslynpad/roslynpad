using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Utilities
{
    [Export(typeof(ITextViewZoomManager))]
    [Shared]
    public class TextViewZoomManager : BaseProxyService<ITextViewZoomManager>, ITextViewZoomManager
    {
        [ImportImplementations(typeof(ITextViewZoomManager))]
        public override IEnumerable<Lazy<ITextViewZoomManager, Orderable>> UnorderedImplementations { get; set; }

        public void ZoomIn(ITextView textView)
        {
            BestImplementation.ZoomIn(textView);
        }

        public void ZoomOut(ITextView textView)
        {
            BestImplementation.ZoomOut(textView);
        }

        public void ZoomTo(ITextView textView, double zoomLevel)
        {
            BestImplementation.ZoomTo(textView, zoomLevel);
        }

        public double ZoomLevel(ITextView textView)
        {
            return BestImplementation.ZoomLevel(textView);
        }
    }
}
