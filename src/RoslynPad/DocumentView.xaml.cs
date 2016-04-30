using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Avalon.Windows.Controls;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.RoslynEditor;

namespace RoslynPad
{
    public partial class DocumentView
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly TextMarkerService _textMarkerService;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private ContextActionsRenderer _contextActionsRenderer;
        private readonly SynchronizationContext _syncContext;
        private RoslynHost _roslynHost;
        private OpenDocumentViewModel _viewModel;

        public DocumentView()
        {
            InitializeComponent();

            _textMarkerService = new TextMarkerService(Editor);
            Editor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
            Editor.TextArea.TextView.LineTransformers.Add(_textMarkerService);
            Editor.Options = new TextEditorOptions
            {
                ConvertTabsToSpaces = true,
                AllowScrollBelowDocument = true,
                IndentationSize = 4
            };

            _syncContext = SynchronizationContext.Current;

            DataContextChanged += OnDataContextChanged;
        }

        private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            _viewModel = (OpenDocumentViewModel)args.NewValue;
            _viewModel.MainViewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;
            _roslynHost = _viewModel.MainViewModel.RoslynHost;

            var avalonEditTextContainer = new AvalonEditTextContainer(Editor);

            _viewModel.PromptForDocument = PromptForDocument;

            await _viewModel.Initialize(
                avalonEditTextContainer,
                a => _syncContext.Post(o => ProcessDiagnostics(a), null),
                text => avalonEditTextContainer.UpdateText(text)
                ).ConfigureAwait(true);

            var documentText = await _viewModel.LoadText().ConfigureAwait(true);
            Editor.AppendText(documentText);
            Editor.Document.UndoStack.ClearAll();
            Editor.Document.TextChanged += (o, e) => _viewModel.SetDirty(Editor.Document.TextLength);

            Editor.TextArea.TextView.LineTransformers.Insert(0, new RoslynHighlightingColorizer(_viewModel.DocumentId, _roslynHost));

            _contextActionsRenderer = new ContextActionsRenderer(Editor, _textMarkerService);
            _contextActionsRenderer.Providers.Add(new RoslynContextActionProvider(_viewModel.DocumentId, _roslynHost));

            Editor.CompletionProvider = new RoslynCodeEditorCompletionProvider(_viewModel.DocumentId, _roslynHost);
        }

        private Task<string> PromptForDocument(PromptForDocumentFlags flags, string currentName)
        {
            // TODO: encapsulate in a dialog service

            var isValid = false;
            var textBox = new TextBox
            {
                MaxLength = 200,
                Text = currentName
            };
            FilterInvalidCharacters(textBox);
            textBox.KeyDown += (sender, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    isValid = true;
                    TaskDialog.CancelCommand.Execute(null, textBox);
                }
            };
            if (flags.HasFlag(PromptForDocumentFlags.AllowNameEdit))
            {
                textBox.Loaded += (sender, args) => textBox.Focus();
            }
            else
            {
                textBox.IsEnabled = false;
            }

            const int saveValue = 10;
            const int dontSaveValue = 20;

            var dialog = new TaskDialog
            {
                Background = Brushes.White,
                Header = "Save Document",
                Content = textBox,
                Buttons =
                {
                    new TaskDialogButtonData(saveValue, "_Save", null, isDefault: true)
                },
            };
            if (flags.HasFlag(PromptForDocumentFlags.ShowDontSave))
            {
                dialog.Buttons.Add(new TaskDialogButtonData(dontSaveValue, "_Don't Save", null));
            }
            dialog.Buttons.Add(new TaskDialogButtonData(TaskDialogButtons.Cancel));
            dialog.ShowInline(this);
            // ReSharper disable once PossibleUnintendedReferenceComparison
            if ((isValid || dialog.Result.ButtonData?.Value == saveValue) && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                return Task.FromResult(textBox.Text);
            }
            if (dialog.Result.ButtonData?.Value != dontSaveValue)
            {
                throw new OperationCanceledException();
            }
            return Task.FromResult<string>(null);
        }

        private static void FilterInvalidCharacters(TextBox textBox)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            textBox.TextChanged += (sender, e) =>
            {
                foreach (var c in e.Changes)
                {
                    if (c.AddedLength == 0) continue;
                    textBox.Select(c.Offset, c.AddedLength);
                    var filteredText = invalidChars.Aggregate(textBox.SelectedText,
                        (current, invalidChar) => current.Replace(invalidChar.ToString(), string.Empty));
                    if (textBox.SelectedText != filteredText)
                    {
                        textBox.SelectedText = filteredText;
                    }
                    textBox.Select(c.Offset + c.AddedLength, 0);
                }
            };
        }

        private void NuGetOnPackageInstalled(NuGetInstallResult installResult)
        {
            if (installResult.References.Count == 0) return;

            var text = string.Join(Environment.NewLine,
                installResult.References.Distinct().Select(r => Path.Combine(MainViewModel.NuGetPathVariableName, r))
                .Concat(installResult.FrameworkReferences.Distinct())
                .Where(r => !_roslynHost.HasReference(_viewModel.DocumentId, r))
                .Select(r => "#r \"" + r + "\"")) + Environment.NewLine;

            Dispatcher.InvokeAsync(() => Editor.Document.Insert(0, text, AnchorMovementType.Default));
        }

        private void ProcessDiagnostics(DiagnosticsUpdatedArgs args)
        {
            _textMarkerService.RemoveAll(x => true);

            foreach (var diagnosticData in args.Diagnostics)
            {
                if (diagnosticData.Severity == DiagnosticSeverity.Hidden || diagnosticData.IsSuppressed)
                {
                    continue;
                }

                var marker = _textMarkerService.TryCreate(diagnosticData.TextSpan.Start, diagnosticData.TextSpan.Length);
                if (marker != null)
                {
                    marker.MarkerColor = GetDiagnosticsColor(diagnosticData);
                    marker.ToolTip = diagnosticData.Message;
                }
            }
        }

        private static Color GetDiagnosticsColor(DiagnosticData diagnosticData)
        {
            switch (diagnosticData.Severity)
            {
                case DiagnosticSeverity.Info:
                    return Colors.LimeGreen;
                case DiagnosticSeverity.Warning:
                    return Colors.DodgerBlue;
                case DiagnosticSeverity.Error:
                    return Colors.Red;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Editor_OnKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.R && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            //{
            //    _roslynHost.GetService<IInlineRenameService>().StartInlineSession(
            //        _roslynHost.CurrentDocument, new TextSpan(Editor.CaretOffset, 1));
            //}
        }

        private void Editor_OnLoaded(object sender, RoutedEventArgs e)
        {
            Editor.Focus();
        }
    }
}
