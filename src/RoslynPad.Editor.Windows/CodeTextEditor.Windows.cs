using ICSharpCode.AvalonEdit.CodeCompletion;
using System.Windows;
using System.Windows.Controls;

namespace RoslynPad.Editor
{
    partial class CodeTextEditor
    {
        partial void Initialize()
        {
            ShowLineNumbers = true;

            MouseHover += OnMouseHover;
            MouseHoverStopped += OnMouseHoverStopped;

            ToolTipService.SetInitialShowDelay(this, 0);
            SearchReplacePanel.Install(this);
        }

        partial void InitializeToolTip()
        {
            _toolTip.Closed += (o, a) => _toolTip = null;
            ToolTipService.SetInitialShowDelay(_toolTip, 0);
            _toolTip.PlacementTarget = this; // required for property inheritance
        }

        partial void InitializeInsightWindow()
        {
            _insightWindow.Style = TryFindResource(typeof(InsightWindow)) as Style;
        }

        partial void InitializeCompletionWindow()
        {
            _completionWindow.Background = CompletionBackground;
        }

        partial class CustomCompletionWindow
        {
            partial void Initialize()
            {
                CompletionList.ListBox.BorderThickness = new Thickness(0);
                CompletionList.ListBox.PreviewMouseDown +=
                    (o, e) => _isSoftSelectionActive = false;
            }
        }
    }
}
