#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using Avalonia;
using Avalonia.Input.TextInput;

using Microsoft.VisualStudio.Text;

using ImeTextSelection = Avalonia.Input.TextInput.TextSelection;

/// <summary>
/// The view's IME integration: composition (preedit) text renders as
/// provisional text at the caret without touching the buffer; the platform commits the
/// final string through the normal text-input path.
/// </summary>
internal sealed class EditorTextInputMethodClient : TextInputMethodClient
{
    private readonly WpfTextView _view;

    public EditorTextInputMethodClient(WpfTextView view)
    {
        _view = view;
        view.Caret.PositionChanged += (_, _) =>
        {
            RaiseCursorRectangleChanged();
            RaiseSurroundingTextChanged();
            RaiseSelectionChanged();
        };
    }

    public override Visual TextViewVisual => _view;

    public override bool SupportsPreedit => true;

    public override bool SupportsSurroundingText => true;

    public override string SurroundingText
    {
        get
        {
            var caret = _view.Caret.Position.BufferPosition;
            return caret.GetContainingLine().GetText();
        }
    }

    public override Rect CursorRectangle
    {
        get
        {
            var caret = _view.Caret;
            return new Rect(
                caret.Left - _view.ViewportLeft,
                caret.Top - _view.ViewportTop,
                caret.Width,
                caret.Height);
        }
    }

    public override ImeTextSelection Selection
    {
        get
        {
            var caret = _view.Caret.Position.BufferPosition;
            var line = caret.GetContainingLine();
            int offset = caret.Position - line.Start.Position;
            return new ImeTextSelection(offset, offset);
        }
        set
        {
            var line = _view.Caret.Position.BufferPosition.GetContainingLine();
            int position = Math.Clamp(line.Start.Position + value.Start, line.Start.Position, line.End.Position);
            _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, position));
        }
    }

    public override void SetPreeditText(string? text)
        => _view.CaretLayerControl.SetPreeditText(string.IsNullOrEmpty(text) ? null : text);
}
