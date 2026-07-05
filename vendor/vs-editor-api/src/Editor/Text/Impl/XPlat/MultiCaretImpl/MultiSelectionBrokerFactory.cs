using System.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.MultiSelection.Implementation
{
    [Export(typeof(IMultiSelectionBrokerFactory))]
    [Export(typeof(IFeatureController))]
    [Shared]
    public class MultiSelectionBrokerFactory : IMultiSelectionBrokerFactory, IFeatureController
    {
        [Import]
        public ISmartIndentationService SmartIndentationService { get; set; }

        [Import]
        public ITextStructureNavigatorSelectorService TextStructureNavigatorSelectorService { get; set; }

        [Import]
        public IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        [Import(AllowDefault = true)]
        public ILoggingServiceInternal LoggingService { get; set; }

        [Import]
        public IFeatureServiceFactory FeatureServiceFactory { get; set; }

        [Import]
        public IEditorOptionsFactoryService EditorOptionsFactoryService { get; set; }

        [Import]
        public IGuardedOperations GuardedOperations { get; set; }

        public IMultiSelectionBroker CreateBroker(ITextView textView)
        {
            return new MultiSelectionBroker(textView, this);
        }
    }
}
