using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Controls;
using RoslynPad.Editor;
using RoslynPad.Runtime;
using RoslynPad.UI;

namespace RoslynPad
{
    public partial class DocumentView : IDisposable
    {
        private readonly SynchronizationContext _syncContext;
        private readonly ErrorMargin _errorMargin;
        private OpenDocumentViewModel _viewModel;
        private IResultObject _contextMenuResultObject;

        public DocumentView()
        {
            InitializeComponent();

            _errorMargin = new ErrorMargin { Visibility = Visibility.Collapsed, MarkerBrush = TryFindResource("ExceptionMarker") as Brush, Width = 10 };
            Editor.TextArea.LeftMargins.Insert(0, _errorMargin);
            Editor.PreviewMouseWheel += EditorOnPreviewMouseWheel;
            Editor.TextArea.Caret.PositionChanged += CaretOnPositionChanged;

            _syncContext = SynchronizationContext.Current;

            DataContextChanged += OnDataContextChanged;
        }

        private void CaretOnPositionChanged(object sender, EventArgs eventArgs)
        {
            Ln.Text = Editor.TextArea.Caret.Line.ToString();
            Col.Text = Editor.TextArea.Caret.Column.ToString();
        }

        private void EditorOnPreviewMouseWheel(object sender, MouseWheelEventArgs args)
        {
            if (_viewModel == null)
            {
                return;
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                _viewModel.MainViewModel.EditorFontSize += args.Delta > 0 ? 1 : -1;
                args.Handled = true;
            }
        }

        private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            _viewModel = (OpenDocumentViewModel)args.NewValue;
            _viewModel.ResultsAvailable += ResultsAvailable;
            _viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;

            _viewModel.EditorFocus += (o, e) => Editor.Focus();

            _viewModel.MainViewModel.EditorFontSizeChanged += OnEditorFontSizeChanged;
            Editor.FontSize = _viewModel.MainViewModel.EditorFontSize;

            var documentText = await _viewModel.LoadText().ConfigureAwait(true);

            var documentId = Editor.Initialize(_viewModel.MainViewModel.RoslynHost, new ClassificationHighlightColors(),
                _viewModel.WorkingDirectory, documentText);

            _viewModel.Initialize(documentId, OnError,
                () => new TextSpan(Editor.SelectionStart, Editor.SelectionLength),
                this);

            Editor.Document.TextChanged += (o, e) => _viewModel.SetDirty();
        }

        private void ResultsAvailable()
        {
            _viewModel.ResultsAvailable -= ResultsAvailable;

            _syncContext.Post(o => ResultPaneRow.Height = new GridLength(1, GridUnitType.Star), null);
        }

        private void OnError(ExceptionResultObject e)
        {
            if (e != null)
            {
                _errorMargin.Visibility = Visibility.Visible;
                _errorMargin.LineNumber = e.LineNumber;
                _errorMargin.Message = "Exception: " + e.Message;
            }
            else
            {
                _errorMargin.Visibility = Visibility.Collapsed;
            }
        }

        private void OnEditorFontSizeChanged(double fontSize)
        {
            Editor.FontSize = fontSize;
        }

        private void NuGetOnPackageInstalled(NuGetInstallResult installResult)
        {
            if (installResult.References.Count == 0) return;

            Dispatcher.InvokeAsync(() =>
            {
                var text = string.Join(Environment.NewLine,
                    installResult.References.Distinct().OrderBy(c => c)
                    .Select(r => Path.Combine(MainViewModel.NuGetPathVariableName, r))
                    .Concat(installResult.FrameworkReferences.Distinct())
                    .Where(r => !_viewModel.MainViewModel.RoslynHost.HasReference(_viewModel.DocumentId, r))
                    .Select(r => "#r \"" + r + "\"")
                    .Where(r => Editor.Text.IndexOf(r, StringComparison.OrdinalIgnoreCase) < 0));

                if (text.Length > 0)
                {
                    Editor.Document.Insert(0, text + Environment.NewLine, AnchorMovementType.Default);
                }
            });
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (e.Key)
                {
                    case Key.T:
                        e.Handled = true;
                        NuGetSearch.Focus();
                        break;
                }
            }
        }

        private void Editor_OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => Editor.Focus(), System.Windows.Threading.DispatcherPriority.Background);
        }

        public void Dispose()
        {
            if (_viewModel?.MainViewModel != null)
            {
                _viewModel.MainViewModel.EditorFontSizeChanged -= OnEditorFontSizeChanged;
            }
        }

        private void OnTreeViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    CopyAllResultsToClipboard(withChildren: true);
                }
                else
                {
                    CopyToClipboard(e.OriginalSource);
                }
            }
            else if (e.Key == Key.Enter)
            {
                TryJumpToLine(e.OriginalSource);
            }
        }

        private void OnTreeViewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TryJumpToLine(e.OriginalSource);
        }

        private void TryJumpToLine(object source)
        {
            var result = (source as FrameworkElement)?.DataContext as CompilationErrorResultObject;
            if (result == null) return;

            Editor.TextArea.Caret.Line = result.Line;
            Editor.TextArea.Caret.Column = result.Column;
            Editor.ScrollToLine(result.Line);

            Dispatcher.InvokeAsync(() => Editor.Focus());
        }

        private void CopyCommand(object sender, ExecutedRoutedEventArgs e)
        {
            CopyToClipboard(e.OriginalSource);
        }

        private void CopyClick(object sender, RoutedEventArgs e)
        {
            CopyToClipboard(sender);
        }

        private void CopyToClipboard(object sender)
        {
            var result = (sender as FrameworkElement)?.DataContext as IResultObject ??
                        _contextMenuResultObject;

            if (result != null)
            {
                Clipboard.SetText(ReferenceEquals(sender, CopyValueWithChildren) ? result.ToString() : result.Value);
            }
        }

        private void CopyAllClick(object sender, RoutedEventArgs e)
        {
            var withChildren = ReferenceEquals(sender, CopyAllValuesWithChildren);

            CopyAllResultsToClipboard(withChildren);
        }

        private void CopyAllResultsToClipboard(bool withChildren)
        {
            var builder = new StringBuilder();
            foreach (var result in _viewModel.ResultsInternal)
            {
                if (withChildren)
                {
                    result.WriteTo(builder);
                    builder.AppendLine();
                }
                else
                {
                    builder.AppendLine(result.Value);
                }
            }

            if (builder.Length > 0)
            {
                Clipboard.SetText(builder.ToString());
            }
        }

        private void ResultTree_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // keyboard-activated
            if (e.CursorLeft < 0 || e.CursorTop < 0)
            {
                _contextMenuResultObject = ResultTree.SelectedItem as IResultObject;
            }
            else
            {
                _contextMenuResultObject = (e.OriginalSource as FrameworkElement)?.DataContext as IResultObject;
            }

            var isResult = _contextMenuResultObject != null;
            CopyValue.IsEnabled = isResult;
            CopyValueWithChildren.IsEnabled = isResult;
        }

        private void SearchTerm_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && _viewModel.NuGet.Packages?.Any() == true)
            {
                if (!_viewModel.NuGet.IsPackagesMenuOpen)
                {
                    _viewModel.NuGet.IsPackagesMenuOpen = true;
                }
                RootNuGetMenu.Focus();
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Editor.Focus();
            }
        }

        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ILViewerTab.IsSelected && ILViewerTab.Content == null)
            {
                var ilViewer = new ILViewer();
                ilViewer.SetBinding(TextElement.FontSizeProperty,
                    nameof(_viewModel.MainViewModel) + "." + nameof(_viewModel.MainViewModel.EditorFontSize));
                ilViewer.SetBinding(ILViewer.TextProperty, nameof(_viewModel.ILText));
                ILViewerTab.Content = ilViewer;
            }
        }
    }
}
