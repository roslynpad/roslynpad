using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Search;
using Ex = System.Linq.Expressions.Expression;

namespace RoslynPad.Editor.Windows
{
#pragma warning disable 618
    public class SearchReplacePanel : SearchPanel
#pragma warning restore 618
    {
        private SearchReplaceInputHandler _handler;
        private TextArea _textArea;

        private static readonly Func<SearchPanel, IEnumerable<TextSegment>> GetSegments = CreateGetSegments();

        private static Func<SearchPanel, IEnumerable<TextSegment>> CreateGetSegments()
        {
            var p = Ex.Parameter(typeof(SearchPanel));
            return Ex.Lambda<Func<SearchPanel, IEnumerable<TextSegment>>>(
                Ex.Property(Ex.Field(p, "renderer"), "CurrentResults"),
                p).Compile();
        }

#pragma warning disable 618

        private SearchReplacePanel()
#pragma warning restore 618
        {
        }

        static SearchReplacePanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchReplacePanel), new FrameworkPropertyMetadata(typeof(SearchReplacePanel)));
        }

        public static readonly DependencyProperty IsReplaceModeProperty = DependencyProperty.Register(
            "IsReplaceMode", typeof(bool), typeof(SearchReplacePanel), new FrameworkPropertyMetadata());

        public bool IsReplaceMode
        {
            get => (bool)GetValue(IsReplaceModeProperty);
            set => SetValue(IsReplaceModeProperty, value);
        }

        public static readonly DependencyProperty ReplacePatternProperty = DependencyProperty.Register(
            "ReplacePattern", typeof(string), typeof(SearchReplacePanel), new FrameworkPropertyMetadata());

        public string ReplacePattern
        {
            get => (string)GetValue(ReplacePatternProperty);
            set => SetValue(ReplacePatternProperty, value);
        }

        /// <summary>
        /// Creates a SearchReplacePanel and installs it to the TextEditor's TextArea.
        /// </summary>
        public new static SearchReplacePanel Install(TextEditor editor)
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));
            return Install(editor.TextArea);
        }

        /// <summary>
        /// Creates a SearchReplacePanel and installs it to the TextArea.
        /// </summary>
        public new static SearchReplacePanel Install(TextArea textArea)
        {
            if (textArea == null)
                throw new ArgumentNullException(nameof(textArea));
            var panel = new SearchReplacePanel { _textArea = textArea };
            typeof(SearchPanel).GetMethod("AttachInternal", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, new object[] { textArea });
            panel.AttachInternal();
            panel._handler = new SearchReplaceInputHandler(textArea, panel);
            textArea.DefaultInputHandler.NestedInputHandlers.Add(panel._handler);
            return panel;
        }

        private void AttachInternal()
        {
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (sender, e) =>
            {
                IsReplaceMode = false;
                Reactivate();
            }));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace, (sender, e) => IsReplaceMode = true));
            CommandBindings.Add(new CommandBinding(SearchCommandsEx.ReplaceNext, (sender, e) => ReplaceNext(), (sender, e) => e.CanExecute = IsReplaceMode));
            CommandBindings.Add(new CommandBinding(SearchCommandsEx.ReplaceAll, (sender, e) => ReplaceAll(), (sender, e) => e.CanExecute = IsReplaceMode));
        }

        public void ReplaceNext()
        {
            if (!IsReplaceMode) return;

            FindNext();
            if (!_textArea.Selection.IsEmpty)
            {
                _textArea.Selection.ReplaceSelectionWithText(ReplacePattern ?? string.Empty);
            }
        }

        public void ReplaceAll()
        {
            if (!IsReplaceMode) return;

            var replacement = ReplacePattern ?? string.Empty;
            var document = _textArea.Document;
            using (document.RunUpdate())
            {
                var segments = GetSegments(this).OrderByDescending(x => x.EndOffset).ToArray();
                foreach (var textSegment in segments)
                {
                    document.Replace(textSegment.StartOffset, textSegment.Length,
                        new StringTextSource(replacement));
                }
            }
        }

        private class SearchReplaceInputHandler : TextAreaInputHandler
        {
            private readonly SearchReplacePanel _panel;

            internal SearchReplaceInputHandler(TextArea textArea, SearchReplacePanel panel)
                : base(textArea)
            {
                RegisterCommands();
                _panel = panel;
            }

            private void RegisterCommands()
            {
                CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, ExecuteFind));
                CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace, ExecuteReplace));
                CommandBindings.Add(new CommandBinding(SearchCommands.FindNext, ExecuteFindNext, CanExecuteWithOpenSearchPanel));
                CommandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious, CanExecuteWithOpenSearchPanel));
                CommandBindings.Add(new CommandBinding(SearchCommandsEx.ReplaceNext, ExecuteReplaceNext, CanExecuteWithOpenSearchPanel));
                CommandBindings.Add(new CommandBinding(SearchCommandsEx.ReplaceAll, ExecuteReplaceAll, CanExecuteWithOpenSearchPanel));
                CommandBindings.Add(new CommandBinding(SearchCommands.CloseSearchPanel, ExecuteCloseSearchPanel, CanExecuteWithOpenSearchPanel));
            }

            private void ExecuteFind(object sender, ExecutedRoutedEventArgs e)
            {
                FindOrReplace(isReplaceMode: false);
            }

            private void ExecuteReplace(object sender, ExecutedRoutedEventArgs e)
            {
                FindOrReplace(isReplaceMode: true);
            }

            private void FindOrReplace(bool isReplaceMode)
            {
                _panel.IsReplaceMode = isReplaceMode;
                _panel.Open();
                if (!TextArea.Selection.IsEmpty && !TextArea.Selection.IsMultiline)
                    _panel.SearchPattern = TextArea.Selection.GetText();
                TextArea.Dispatcher.InvokeAsync(() => _panel.Reactivate(), DispatcherPriority.Input);
            }

            private void CanExecuteWithOpenSearchPanel(object sender, CanExecuteRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                {
                    e.CanExecute = false;
                    e.ContinueRouting = true;
                }
                else
                {
                    e.CanExecute = true;
                    e.Handled = true;
                }
            }

            private void ExecuteFindNext(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.FindNext();
                e.Handled = true;
            }

            private void ExecuteFindPrevious(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.FindPrevious();
                e.Handled = true;
            }

            private void ExecuteReplaceNext(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.ReplaceNext();
                e.Handled = true;
            }

            private void ExecuteReplaceAll(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.ReplaceAll();
                e.Handled = true;
            }

            private void ExecuteCloseSearchPanel(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.Close();
                e.Handled = true;
            }
        }
    }

    public static class SearchCommandsEx
    {
        /// <summary>Replaces the next occurrence in the document.</summary>
        public static readonly RoutedCommand ReplaceNext = new RoutedCommand("ReplaceNext", typeof(SearchReplacePanel),
            new InputGestureCollection
            {
                new KeyGesture(Key.R, ModifierKeys.Alt)
            });

        /// <summary>Replaces all the occurrences in the document.</summary>
        public static readonly RoutedCommand ReplaceAll = new RoutedCommand("ReplaceAll", typeof(SearchPanel),
            new InputGestureCollection
            {
                new KeyGesture(Key.A, ModifierKeys.Alt)
            });
    }
}