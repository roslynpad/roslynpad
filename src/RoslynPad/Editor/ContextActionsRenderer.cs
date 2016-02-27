// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace RoslynPad.Editor
{
    internal interface IContextActionProvider
    {
        Task<IEnumerable<object>> GetActions(int offset, int length, CancellationToken cancellationToken);

        ICommand GetActionCommand(object action);
    }

    internal sealed class ContextActionsRenderer : IDisposable
    {
        private const int DelayMoveMilliseconds = 500;

        private readonly ObservableCollection<IContextActionProvider> _providers;
        private readonly CodeTextEditor _editor;
        private readonly TextMarkerService _textMarkerService;
        private readonly DispatcherTimer _delayMoveTimer;

        private ContextActionsBulbPopup _popup;
        private CancellationTokenSource _cancellationTokenSource;
        private IEnumerable<object> _actions;

        public ContextActionsRenderer(CodeTextEditor editor, TextMarkerService textMarkerService)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            _editor = editor;
            _textMarkerService = textMarkerService;

            editor.TextArea.Caret.PositionChanged += CaretPositionChanged;

            editor.KeyDown += ContextActionsRenderer_KeyDown;
            _providers = new ObservableCollection<IContextActionProvider>();
            _providers.CollectionChanged += providers_CollectionChanged;

            editor.TextArea.TextView.ScrollOffsetChanged += ScrollChanged;
            _delayMoveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DelayMoveMilliseconds) };
            _delayMoveTimer.Stop();
            _delayMoveTimer.Tick += TimerMoveTick;
        }

        public void Dispose()
        {
            ClosePopup();
        }

        public IList<IContextActionProvider> Providers => _providers;

        private void providers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            StartTimer();
        }

        private async void ContextActionsRenderer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.OemPeriod || Keyboard.Modifiers != ModifierKeys.Control) return;

            CreatePopup();
            if (_popup.IsOpen && _popup.ItemsSource != null)
            {
                _popup.IsMenuOpen = true;
                _popup.Focus();
            }
            else
            {
                ClosePopup();
                if (!await LoadActionsWithCancellationAsync().ConfigureAwait(true)) return;
                _popup.ItemsSource = _actions;
                if (_popup.HasItems)
                {
                    _popup.IsMenuOpen = true;
                    _popup.OpenAtLineStart(_editor);
                    _popup.Focus();
                }
            }
        }

        private void CreatePopup()
        {
            if (_popup == null)
            {
                _popup = new ContextActionsBulbPopup(_editor.TextArea) {CommandProvider = GetActionCommand};
            }
        }

        private async Task<bool> LoadActionsWithCancellationAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                _actions = await LoadActionsAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException)
            {
            }
            _cancellationTokenSource = null;
            return false;
        }

        private ICommand GetActionCommand(object action)
        {
            return _providers.Select(provider => provider.GetActionCommand(action))
                .FirstOrDefault(command => command != null);
        }

        private async Task<IEnumerable<object>> LoadActionsAsync(CancellationToken cancellationToken)
        {
            var allActions = new List<object>();
            foreach (var provider in _providers)
            {
                var marker = _textMarkerService.GetMarkersAtOffset(_editor.TextArea.Caret.Offset).FirstOrDefault();
                if (marker == null) continue;
                var actions = await provider.GetActions(marker.StartOffset, marker.Length, cancellationToken).ConfigureAwait(true);
                allActions.AddRange(actions);
            }
            return allActions;
        }

        private void ScrollChanged(object sender, EventArgs e)
        {
            StartTimer();
        }

        private async void TimerMoveTick(object sender, EventArgs e)
        {
            if (!_delayMoveTimer.IsEnabled)
                return;
            ClosePopup();

            // Don't show the context action popup when the caret is outside the editor boundaries
            var textView = _editor.TextArea.TextView;
            var editorRect = new Rect((Point)textView.ScrollOffset, textView.RenderSize);
            var caretRect = _editor.TextArea.Caret.CalculateCaretRectangle();
            if (!editorRect.Contains(caretRect))
                return;

            // Don't show the context action popup when the text editor is invisible, i.e., the Forms Designer is active.
            if (PresentationSource.FromVisual(textView) == null) return;

            if (!await LoadActionsWithCancellationAsync().ConfigureAwait(true)) return;

            CreatePopup();
            _popup.ItemsSource = _actions;
            if (_popup.HasItems)
            {
                _popup.OpenAtLineStart(_editor);
            }
        }

        private void CaretPositionChanged(object sender, EventArgs e)
        {
            StartTimer();
        }

        private void StartTimer()
        {
            ClosePopup();
            if (_providers.Count == 0)
                return;
            _delayMoveTimer.Start();
        }

        private void ClosePopup()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            _delayMoveTimer.Stop();
            if (_popup != null)
            {
                _popup.Close();
                _popup.IsMenuOpen = false;
                _popup.ItemsSource = null;
            }
        }
    }

    internal class ExtendedPopup : Popup
    {
        private readonly UIElement _parent;

        public ExtendedPopup(UIElement parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            _parent = parent;
        }

        public new bool IsOpen => base.IsOpen;

        private bool _openIfFocused;

        public bool IsOpenIfFocused
        {
            get { return _openIfFocused; }
            set
            {
                if (_openIfFocused != value)
                {
                    _openIfFocused = value;
                    if (value)
                    {
                        _parent.IsKeyboardFocusedChanged += parent_IsKeyboardFocusedChanged;
                    }
                    else {
                        _parent.IsKeyboardFocusedChanged -= parent_IsKeyboardFocusedChanged;
                    }
                    OpenOrClose();
                }
            }
        }

        private void parent_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            OpenOrClose();
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);
            OpenOrClose();
        }

        private void OpenOrClose()
        {
            var newIsOpen = _openIfFocused && (_parent.IsKeyboardFocused || IsKeyboardFocusWithin);
            base.IsOpen = newIsOpen;
        }
    }

    internal sealed class ContextActionsBulbPopup : ExtendedPopup
    {
        private readonly MenuItem _mainItem;

        public ContextActionsBulbPopup(UIElement parent) : base(parent)
        {
            UseLayoutRounding = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            StaysOpen = true;
            AllowsTransparency = true;
            _mainItem = new MenuItem
            {
                ItemContainerStyle = CreateItemContainerStyle(),
                Header = new Image
                {
                    Source = TryFindResource("Bulb") as ImageSource,
                    Width = 16,
                    Height = 16
                }
            };
            var menu = new Menu
            {
                Background = Brushes.Transparent,
                BorderBrush = _mainItem.BorderBrush,
                BorderThickness = _mainItem.BorderThickness,
                Items = { _mainItem }
            };
            Child = menu;
        }

        private Style CreateItemContainerStyle()
        {
            var style = new Style(typeof(MenuItem));
            style.Setters.Add(new Setter(MenuItem.CommandProperty,
                new Binding { Converter = new ActionCommandConverter(this) }));
            style.Seal();
            return style;
        }

        public IEnumerable<object> ItemsSource
        {
            get { return (IEnumerable<object>)_mainItem.ItemsSource; }
            set { _mainItem.ItemsSource = value; }
        }

        public bool HasItems => _mainItem.HasItems;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
                IsOpenIfFocused = false;
        }

        public void Close()
        {
            IsOpenIfFocused = false;
        }

        public bool IsMenuOpen
        {
            get { return _mainItem.IsSubmenuOpen; }
            set { _mainItem.IsSubmenuOpen = value; }
        }

        public Func<object, ICommand> CommandProvider { get; set; }

        public new void Focus()
        {
            Child.Focus();
        }

        public void OpenAtLineStart(CodeTextEditor editor)
        {
            SetPosition(this, editor, editor.TextArea.Caret.Line, 1);
            VerticalOffset -= 16;
            IsOpenIfFocused = true;
        }

        private static void SetPosition(Popup popup, TextEditor editor, int line, int column, bool openAtWordStart = false)
        {
            var document = editor.Document;
            var offset = document.GetOffset(line, column);
            if (openAtWordStart)
            {
                var wordStart = document.FindPreviousWordStart(offset);
                if (wordStart != -1)
                {
                    var wordStartLocation = document.GetLocation(wordStart);
                    line = wordStartLocation.Line;
                    column = wordStartLocation.Column;
                }
            }
            var caretScreenPos = editor.TextArea.TextView.GetScreenPosition(line, column);
            popup.HorizontalOffset = caretScreenPos.X;
            popup.VerticalOffset = caretScreenPos.Y;
            popup.Placement = PlacementMode.Absolute;
        }

        private class ActionCommandConverter : IValueConverter
        {
            private readonly ContextActionsBulbPopup _owner;

            public ActionCommandConverter(ContextActionsBulbPopup owner)
            {
                _owner = owner;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return _owner.CommandProvider?.Invoke(value);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

    }

    internal static class DocumentUtilities
    {
        public static int FindPreviousWordStart(this ITextSource textSource, int offset)
        {
            return TextUtilities.GetNextCaretPosition(textSource, offset, LogicalDirection.Backward, CaretPositioningMode.WordStart);
        }
    }

    internal static class TextViewExtensions
    {
        public static Point GetScreenPosition(this TextView textView, int line, int column)
        {
            var visualPosition = textView.GetVisualPosition(
                new TextViewPosition(line, column), VisualYPosition.LineBottom) - textView.ScrollOffset;
            var positionInPixels = textView.PointToScreen(new Point(visualPosition.X.CoerceValue(0, textView.ActualWidth),
                                                                    visualPosition.Y.CoerceValue(0, textView.ActualHeight)));
            return positionInPixels.TransformFromDevice(textView);
        }

        private static double CoerceValue(this double value, double minimum, double maximum)
        {
            return Math.Max(Math.Min(value, maximum), minimum);
        }

        private static Point TransformFromDevice(this Point point, Visual visual)
        {
            var compositionTarget = PresentationSource.FromVisual(visual)?.CompositionTarget;
            if (compositionTarget == null) throw new InvalidOperationException("Invalid visual");
            var matrix = compositionTarget.TransformFromDevice;
            return new Point(point.X * matrix.M11, point.Y * matrix.M22);
        }
    }
}
