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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
#if AVALONIA
using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using ImageSource = Avalonia.Media.Imaging.IBitmap;
#else
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
#endif

namespace RoslynPad.Editor
{
    public sealed class ContextActionsRenderer : IDisposable
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
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _textMarkerService = textMarkerService;

            editor.TextArea.Caret.PositionChanged += CaretPositionChanged;

            editor.KeyDown += ContextActionsRenderer_KeyDown;
            _providers = new ObservableCollection<IContextActionProvider>();
            _providers.CollectionChanged += providers_CollectionChanged;

            editor.TextArea.TextView.ScrollOffsetChanged += ScrollChanged;
            _delayMoveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DelayMoveMilliseconds) };
            _delayMoveTimer.Stop();
            _delayMoveTimer.Tick += TimerMoveTick;

            editor.HookupLoadedUnloadedAction(HookupWindowMove);
        }

        public ImageSource IconImage { get; set; }

        private void HookupWindowMove(bool enable)
        {
            var window = _editor.GetWindow();
            if (window != null)
            {
                window.DetachLocationChanged(WindowOnLocationChanged);
                if (enable)
                {
                    window.AttachLocationChanged(WindowOnLocationChanged);
                }
            }
        }
        
        private void WindowOnLocationChanged(object sender, EventArgs eventArgs)
        {
            if (_popup?.IsOpen == true)
            {
                _popup.HorizontalOffset += double.Epsilon;
                _popup.HorizontalOffset -= double.Epsilon;
            }
        }

        public void Dispose()
        {
            var window = _editor.GetWindow();
            if (window != null)
            {
                window.DetachLocationChanged(WindowOnLocationChanged);
            }

            ClosePopup();
        }

        public IList<IContextActionProvider> Providers => _providers;

        private void providers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            StartTimer();
        }

        private async void ContextActionsRenderer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.OemPeriod ||
#if AVALONIA
                e.Modifiers != InputModifiers.Control
#else
                Keyboard.Modifiers != ModifierKeys.Control
#endif
                ) return;

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
                _popup = new ContextActionsBulbPopup(_editor.TextArea)
                {
                    CommandProvider = GetActionCommand,
                    Icon = IconImage,
                };

                // TODO: workaround to refresh menu with latest document
                _popup.MenuOpened += async (sender, args) =>
                {
                    if (await LoadActionsWithCancellationAsync().ConfigureAwait(true))
                    {
                        _popup.ItemsSource = _actions;
                    }
                };

                _popup.MenuClosed += (sender, args) =>
                {
                    _editor.GetDispatcher().InvokeAsync(() => _editor.Focus(), DispatcherPriority.Background);
                };
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
            catch (Exception)
            {
                // ignored
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
                var offset = _editor.TextArea.Caret.Offset;
                var length = 0;
                var marker = _textMarkerService.GetMarkersAtOffset(offset).FirstOrDefault();
                if (marker != null)
                {
                    offset = marker.StartOffset;
                    length = marker.Length;
                }
                var actions = await provider.GetActions(offset, length, cancellationToken).ConfigureAwait(true);
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
            var editorRect = new Rect((Point)textView.ScrollOffset, textView.GetRenderSize());
            var caretRect = _editor.TextArea.Caret.CalculateCaretRectangle();
            if (!editorRect.Contains(caretRect))
                return;

#if !AVALONIA
            // Don't show the context action popup when the text editor is invisible, i.e., the Forms Designer is active.
            if (PresentationSource.FromVisual(textView) == null) return;
#endif

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
}
