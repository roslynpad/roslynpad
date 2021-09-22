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
using System.Windows.Input;
#if AVALONIA
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Threading;
using ImageSource = Avalonia.Media.IImage;
using ModifierKeys = Avalonia.Input.KeyModifiers;
#else
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
#endif

namespace RoslynPad.Editor
{
    public sealed class ContextActionsRenderer
    {
        private const int DelayMoveMilliseconds = 500;

        private readonly ObservableCollection<IContextActionProvider> _providers;
        private readonly CodeTextEditor _editor;
        private readonly TextMarkerService _textMarkerService;
        private readonly MarkerMargin _bulbMargin;
        private readonly DispatcherTimer _delayMoveTimer;
        private readonly ContextActionsBulbContextMenu _contextMenu;

        private CancellationTokenSource? _cancellationTokenSource;
        private List<object>? _actions;
        private ImageSource? _iconImage;

        public ContextActionsRenderer(CodeTextEditor editor, TextMarkerService textMarkerService)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _textMarkerService = textMarkerService;

            _contextMenu = CreateContextMenu();
            _bulbMargin = new MarkerMargin { Width = 16, Margin = new Thickness(0, 0, 5, 0) };
            _bulbMargin.MarkerPointerDown += (o, e) => OpenContextMenu();
            var index = editor.TextArea.LeftMargins.Count > 0 ? editor.TextArea.LeftMargins.Count - 1 : 0;
            editor.TextArea.LeftMargins.Insert(index, _bulbMargin);

            editor.TextArea.Caret.PositionChanged += CaretPositionChanged;

            editor.KeyDown += ContextActionsRenderer_KeyDown;
            _providers = new ObservableCollection<IContextActionProvider>();
            _providers.CollectionChanged += Providers_CollectionChanged;

            editor.TextArea.TextView.ScrollOffsetChanged += ScrollChanged;
            _delayMoveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DelayMoveMilliseconds) };
            _delayMoveTimer.Stop();
            _delayMoveTimer.Tick += TimerMoveTick;
        }

        public ImageSource? IconImage
        {
            get => _iconImage;
            set
            {
                _bulbMargin.MarkerImage = value;
                _iconImage = value;
            }
        }

        public IList<IContextActionProvider> Providers => _providers;

        private void Providers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => StartTimer();

        private async void ContextActionsRenderer_KeyDown(object sender, KeyEventArgs e)
        {
            if (!(e.Key == Key.OemPeriod && e.HasModifiers(ModifierKeys.Control))) return;

            Cancel();
            if (!await LoadActionsWithCancellationAsync().ConfigureAwait(true) ||
                _actions?.Count < 1)
            {
                HideBulb();
                return;
            }

            _contextMenu.SetItems(_actions!);
            _bulbMargin.LineNumber = _editor.TextArea.Caret.Line;
            OpenContextMenu();
        }

        private void OpenContextMenu()
        {
            _contextMenu.Open(_bulbMargin.Marker);
#if !AVALONIA
            _contextMenu.Focus();
#endif
        }

        private ContextActionsBulbContextMenu CreateContextMenu()
        {
            var contextMenu = new ContextActionsBulbContextMenu(new ActionCommandConverter(GetActionCommand));

            // TODO: workaround to refresh menu with latest document
            contextMenu.ContextMenuOpening += async (sender, args) =>
            {
                if (await LoadActionsWithCancellationAsync().ConfigureAwait(true))
                {
                    var popup = (ContextActionsBulbContextMenu)sender!;
                    popup.SetItems(_actions!);
                }
            };

            return contextMenu;
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

        private ICommand? GetActionCommand(object action) =>
            _providers.Select(provider => provider.GetActionCommand(action))
                .FirstOrDefault(command => command != null);

        private async Task<List<object>> LoadActionsAsync(CancellationToken cancellationToken)
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

        private void ScrollChanged(object? sender, EventArgs e) => StartTimer();

        private async void TimerMoveTick(object? sender, EventArgs e)
        {
            if (!_delayMoveTimer.IsEnabled)
                return;

            Cancel();

            // Don't show the context action popup when the caret is outside the editor boundaries
            var textView = _editor.TextArea.TextView;
            var editorRect = new Rect((Point)textView.ScrollOffset, textView.GetRenderSize());
            var caretRect = _editor.TextArea.Caret.CalculateCaretRectangle();
            if (!editorRect.Contains(caretRect))
            {
                HideBulb();
                return;
            }

            if (!await LoadActionsWithCancellationAsync().ConfigureAwait(true) ||
                _actions?.Count < 1)
            {
                HideBulb();
                return;
            }

            _contextMenu.SetItems(_actions!);
            _bulbMargin.LineNumber = _editor.TextArea.Caret.Line;
        }

        private void HideBulb() => _bulbMargin.LineNumber = null;

        private void CaretPositionChanged(object? sender, EventArgs e) => StartTimer();

        private void StartTimer()
        {
            if (_providers.Count == 0)
                return;
            _delayMoveTimer.Start();
        }

        private void Cancel()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            _delayMoveTimer.Stop();
        }
    }

    internal class ActionCommandConverter : IValueConverter
    {
        public ActionCommandConverter(Func<object, ICommand?>? commandProvider) => CommandProvider = commandProvider;

        public Func<object, ICommand?>? CommandProvider { get; }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) => CommandProvider?.Invoke(value);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
