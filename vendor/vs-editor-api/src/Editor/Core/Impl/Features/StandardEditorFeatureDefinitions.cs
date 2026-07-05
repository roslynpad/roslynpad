using System.Composition;

namespace Microsoft.VisualStudio.Utilities.Features.Implementation
{
    /// <summary>
    /// Contains exports for <see cref="FeatureDefinition"/>s shared in <see cref="PredefinedEditorFeatureNames"/>
    /// </summary>
    [Shared]
    public class StandardEditorFeatureDefinitions
    {
        [Export]
        [Name(PredefinedEditorFeatureNames.Editor)]
        public FeatureDefinition EditorDefinition { get; set; }

        [Export]
        [Name(PredefinedEditorFeatureNames.Popup)]
        public FeatureDefinition PopupDefinition { get; set; }

        [Export]
        [Name(PredefinedEditorFeatureNames.InteractivePopup)]
        [BaseDefinition(PredefinedEditorFeatureNames.Popup)]
        public FeatureDefinition InteractivePopupDefinition { get; set; }

        [Export]
        [Name(PredefinedEditorFeatureNames.Completion)]
        [BaseDefinition(PredefinedEditorFeatureNames.InteractivePopup)]
        [BaseDefinition(PredefinedEditorFeatureNames.Editor)]
        public FeatureDefinition CompletionDefinition { get; set; }

        [Export]
        [Name(PredefinedEditorFeatureNames.AsyncCompletion)]
        [BaseDefinition(PredefinedEditorFeatureNames.Completion)]
        public FeatureDefinition AsyncCompletionDefinition { get; set; }
    }
}
