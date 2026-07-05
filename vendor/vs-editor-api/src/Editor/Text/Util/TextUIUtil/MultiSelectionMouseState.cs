using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.MultiSelection;

namespace Microsoft.VisualStudio.Text.UI.Utilities
{
    public class MultiSelectionMouseState
    {
        public static MultiSelectionMouseState GetStateForView(ITextView textView)
        {
            return textView.Properties.GetOrCreateSingletonProperty(() =>
            {
                return new MultiSelectionMouseState(textView);
            });
        }

        private MultiSelectionMouseState(ITextView textView)
        {
            _textView = textView;
            textView.LayoutChanged += OnLayoutChanged;
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (_provisionalSelection != Selection.Invalid)
            {
                _provisionalSelection = _provisionalSelection.MapToSnapshot(e.NewSnapshot, _textView);
            }
        }

        private Selection _provisionalSelection = Selection.Invalid;
        private ITextView _textView;

        public Selection ProvisionalSelection
        {
            get
            {
                return _provisionalSelection;
            }
            set
            {
                if (_provisionalSelection != value)
                {
                    _provisionalSelection = value;
                    FireProvisionalSelectionChanged();
                }
            }
        }

        public event EventHandler ProvisionalSelectionChanged;

        private void FireProvisionalSelectionChanged()
        {
            ProvisionalSelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool UserIsDraggingSelection { get; set; } = false;
    }
}
