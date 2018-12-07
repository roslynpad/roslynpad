using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using NuGet.Versioning;
using RoslynPad.Hosting;
using RoslynPad.Roslyn.Rename;
using RoslynPad.Runtime;
using RoslynPad.Utilities;

namespace RoslynPad.UI
{
    [Export]
    public class OpenDocumentViewModel : NotificationObject
    {
        private const string DefaultILText = "// Run to view IL";

        private readonly IServiceProvider _serviceProvider;
        private readonly IAppDispatcher _dispatcher;
        private readonly ITelemetryProvider _telemetryProvider;
        private ExecutionHost _executionHost;
        private ObservableCollection<IResultObject> _results;
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private Action<object> _executionHostOnDumped;
        private bool _isDirty;
        private ExecutionPlatform _platform;
        private bool _isSaving;
        private IDisposable _viewDisposable;
        private Action<ExceptionResultObject> _onError;
        private Func<TextSpan> _getSelection;
        private string _ilText;
        private bool _isInitialized;
        private bool _isLiveMode;
        private Timer _liveModeTimer;
        private InitializationParameters _executionHostParameters;

        public string WorkingDirectory => Document != null
            ? Path.GetDirectoryName(Document.Path)
            : MainViewModel.DocumentRoot.Path;

        public IEnumerable<object> Results => _results;

        internal ObservableCollection<IResultObject> ResultsInternal
        {
            // ReSharper disable once UnusedMember.Local
            get => _results;
            private set
            {
                _results = value;
                OnPropertyChanged(nameof(Results));
            }
        }

        public IDelegateCommand ToggleLiveModeCommand { get; }

        public bool IsLiveMode
        {
            get => _isLiveMode;
            private set
            {
                if (!SetProperty(ref _isLiveMode, value)) return;
                RunCommand.RaiseCanExecuteChanged();

                if (value)
                {
                    // ReSharper disable once UnusedVariable
                    var task = Run();

                    if (_liveModeTimer == null)
                    {
                        _liveModeTimer = new Timer(o => _dispatcher.InvokeAsync(() =>
                        {
                            // ReSharper disable once UnusedVariable
                            var runTask = Run();
                        }), null, Timeout.Infinite, Timeout.Infinite);
                    }
                }
            }
        }

        public DocumentViewModel Document { get; private set; }

        public string ILText
        {
            get => _ilText;
            private set => SetProperty(ref _ilText, value);
        }

        [ImportingConstructor]
        public OpenDocumentViewModel(IServiceProvider serviceProvider, MainViewModelBase mainViewModel, ICommandProvider commands, IAppDispatcher appDispatcher, ITelemetryProvider telemetryProvider)
        {
            _serviceProvider = serviceProvider;
            MainViewModel = mainViewModel;
            CommandProvider = commands;

            NuGet = serviceProvider.GetService<NuGetDocumentViewModel>();
            NuGet.RestoreCompleted += OnNuGetRestoreCompleted;

            _dispatcher = appDispatcher;
            _telemetryProvider = telemetryProvider;
            AvailablePlatforms = serviceProvider.GetService<IPlatformsFactory>()
                .GetExecutionPlatforms().ToImmutableArray();

            SaveCommand = commands.CreateAsync(() => Save(promptSave: false));
            RunCommand = commands.CreateAsync(Run, () => !IsRunning && Platform != null);
            CompileAndSaveCommand = commands.CreateAsync(CompileAndSave, () => Platform != null);
            RestartHostCommand = commands.CreateAsync(RestartHost, () => Platform != null);
            FormatDocumentCommand = commands.CreateAsync(FormatDocument);
            CommentSelectionCommand = commands.CreateAsync(() => CommentUncommentSelection(CommentAction.Comment));
            UncommentSelectionCommand = commands.CreateAsync(() => CommentUncommentSelection(CommentAction.Uncomment));
            RenameSymbolCommand = commands.CreateAsync(RenameSymbol);
            ToggleLiveModeCommand = commands.Create(() => IsLiveMode = !IsLiveMode);

            ILText = DefaultILText;
        }

        private void OnNuGetRestoreCompleted(NuGetRestoreResult restoreResult)
        {
            var host = MainViewModel.RoslynHost;
            var document = host.GetDocument(DocumentId);
            var project = document.Project;

            var nugetReferences = restoreResult.CompileReferences.Select(x => host.CreateMetadataReference(x));
            var references = host.DefaultReferences.AddRange(nugetReferences);

            project = project.WithMetadataReferences(references);
            document = project.GetDocument(DocumentId);

            host.UpdateDocument(document);

            _executionHostParameters.References = GetReferencePaths(references);
            var task = _executionHost.Update(_executionHostParameters);
        }

        public event Action ResultsAvailable;

        private void ExecutionHostOnError(ExceptionResultObject errorResult)
        {
            _dispatcher.InvokeAsync(() =>
            {
                _onError?.Invoke(errorResult);
                if (errorResult != null)
                {
                    ResultsInternal?.Add(errorResult);

                    ResultsAvailable?.Invoke();
                }
            });
        }

        private void ExecutionHostOnCompilationErrors(List<CompilationErrorResultObject> errors)
        {
            _dispatcher.InvokeAsync(() =>
            {
                foreach (var error in errors)
                {
                    ResultsInternal?.Add(error);
                }

                ResultsAvailable?.Invoke();
            });
        }

        private void ExecutionHostOnDisassembled(string il)
        {
            ILText = il;
        }

        public void SetDocument(DocumentViewModel document)
        {
            Document = document == null ? null : DocumentViewModel.FromPath(document.Path);

            IsDirty = document?.IsAutoSave == true;

            var roslynHost = MainViewModel.RoslynHost;

            _executionHostParameters = new InitializationParameters(
                GetReferencePaths(roslynHost.DefaultReferences),
                roslynHost.DefaultImports, WorkingDirectory);
            _executionHost = new ExecutionHost(_executionHostParameters);

            _executionHost.Error += ExecutionHostOnError;
            _executionHost.CompilationErrors += ExecutionHostOnCompilationErrors;
            _executionHost.Disassembled += ExecutionHostOnDisassembled;

            Platform = AvailablePlatforms.FirstOrDefault();
        }

        private IList<string> GetReferencePaths(IEnumerable<MetadataReference> references)
        {
            return references.OfType<PortableExecutableReference>().Select(x => x.FilePath).ToImmutableArray();
        }

        private async Task RenameSymbol()
        {
            var host = MainViewModel.RoslynHost;
            var document = host.GetDocument(DocumentId);
            var symbol = await RenameHelper.GetRenameSymbol(document, _getSelection().Start).ConfigureAwait(true);
            if (symbol == null) return;

            var dialog = _serviceProvider.GetService<IRenameSymbolDialog>();
            dialog.Initialize(symbol.Name);
            await dialog.ShowAsync();
            if (dialog.ShouldRename)
            {
                var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, symbol, dialog.SymbolName, null).ConfigureAwait(true);
                var newDocument = newSolution.GetDocument(DocumentId);
                // TODO: possibly update entire solution
                host.UpdateDocument(newDocument);
            }
            OnEditorFocus();
        }

        private enum CommentAction
        {
            Comment,
            Uncomment
        }

        private async Task CommentUncommentSelection(CommentAction action)
        {
            const string singleLineCommentString = "//";
            var document = MainViewModel.RoslynHost.GetDocument(DocumentId);
            var selection = _getSelection();
            var documentText = await document.GetTextAsync().ConfigureAwait(false);
            var changes = new List<TextChange>();
            var lines = documentText.Lines.SkipWhile(x => !x.Span.IntersectsWith(selection))
                .TakeWhile(x => x.Span.IntersectsWith(selection)).ToArray();

            if (action == CommentAction.Comment)
            {
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(documentText.GetSubText(line.Span).ToString()))
                    {
                        changes.Add(new TextChange(new TextSpan(line.Start, 0), singleLineCommentString));
                    }
                }
            }
            else if (action == CommentAction.Uncomment)
            {
                foreach (var line in lines)
                {
                    var text = documentText.GetSubText(line.Span).ToString();
                    if (text.TrimStart().StartsWith(singleLineCommentString, StringComparison.Ordinal))
                    {
                        changes.Add(new TextChange(new TextSpan(
                            line.Start + text.IndexOf(singleLineCommentString, StringComparison.Ordinal),
                            singleLineCommentString.Length), string.Empty));
                    }
                }
            }

            if (changes.Count == 0) return;

            MainViewModel.RoslynHost.UpdateDocument(document.WithText(documentText.WithChanges(changes)));
            if (action == CommentAction.Uncomment)
            {
                await FormatDocument().ConfigureAwait(false);
            }
        }

        private async Task FormatDocument()
        {
            var document = MainViewModel.RoslynHost.GetDocument(DocumentId);
            var formattedDocument = await Formatter.FormatAsync(document).ConfigureAwait(false);
            MainViewModel.RoslynHost.UpdateDocument(formattedDocument);
        }

        public IReadOnlyList<ExecutionPlatform> AvailablePlatforms { get; }

        public ExecutionPlatform Platform
        {
            get => _platform;
            set
            {
                if (_executionHost == null || value == null) throw new InvalidOperationException();

                if (SetProperty(ref _platform, value))
                {
                    _executionHost.HostPath = value.HostPath;
                    _executionHost.HostArguments = value.HostArguments;

                    if (_isInitialized)
                    {
                        NuGet.SetTargetFramework(value.TargetFrameworkName);
                        RestartHostCommand?.Execute();
                    }
                }
            }
        }

        private async Task RestartHost()
        {
            Reset();
            try
            {
                await Task.Run(() => _executionHost.ResetAsync()).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _telemetryProvider.ReportError(e);
                throw;
            }
            finally
            {
                SetIsRunning(false);
            }
        }

        private void SetIsRunning(bool value)
        {
            _dispatcher.InvokeAsync(() => IsRunning = value);
        }

        public async Task AutoSave()
        {
            if (!IsDirty) return;
            if (Document == null)
            {
                var index = 1;
                string path;
                do
                {
                    path = Path.Combine(WorkingDirectory, DocumentViewModel.GetAutoSaveName("Program" + index++));
                } while (File.Exists(path));
                Document = DocumentViewModel.FromPath(path);
            }

            await SaveDocument(Document.GetAutoSavePath()).ConfigureAwait(false);
        }

        public async Task<SaveResult> Save(bool promptSave)
        {
            if (_isSaving) return SaveResult.Cancel;
            if (!IsDirty && promptSave) return SaveResult.Save;

            _isSaving = true;
            try
            {
                var result = SaveResult.Save;
                if (Document == null || Document.IsAutoSaveOnly)
                {
                    var dialog = _serviceProvider.GetService<ISaveDocumentDialog>();
                    dialog.ShowDontSave = promptSave;
                    dialog.AllowNameEdit = true;
                    dialog.FilePathFactory = s => DocumentViewModel.GetDocumentPathFromName(WorkingDirectory, s);
                    await dialog.ShowAsync();
                    result = dialog.Result;
                    if (result == SaveResult.Save)
                    {
                        Document?.DeleteAutoSave();
                        Document = MainViewModel.AddDocument(dialog.DocumentName);
                        OnPropertyChanged(nameof(Title));
                    }
                }
                else if (promptSave)
                {
                    var dialog = _serviceProvider.GetService<ISaveDocumentDialog>();
                    dialog.ShowDontSave = true;
                    dialog.DocumentName = Document.Name;
                    await dialog.ShowAsync();
                    result = dialog.Result;
                }

                if (result == SaveResult.Save)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    await SaveDocument(Document.GetSavePath()).ConfigureAwait(true);
                    IsDirty = false;
                }

                if (result != SaveResult.Cancel)
                {
                    Document?.DeleteAutoSave();
                }

                return result;
            }
            finally
            {
                _isSaving = false;
            }
        }

        private async Task SaveDocument(string path)
        {
            if (DocumentId == null) return;

            var text = await MainViewModel.RoslynHost.GetDocument(DocumentId).GetTextAsync().ConfigureAwait(false);
            using (var writer = File.CreateText(path))
            {
                for (int lineIndex = 0; lineIndex < text.Lines.Count - 1; ++lineIndex)
                {
                    var lineText = text.Lines[lineIndex].ToString();
                    await writer.WriteLineAsync(lineText).ConfigureAwait(false);
                }
                await writer.WriteAsync(text.Lines[text.Lines.Count - 1].ToString()).ConfigureAwait(false);
            }
        }

        internal void Initialize(DocumentId documentId,
            Action<ExceptionResultObject> onError,
            Func<TextSpan> getSelection, IDisposable viewDisposable)
        {
            _viewDisposable = viewDisposable;
            _onError = onError;
            _getSelection = getSelection;
            DocumentId = documentId;
            _isInitialized = true;

            NuGet.SetTargetFramework(Platform.TargetFrameworkName);
            var task = UpdatePackages();
            RestartHostCommand?.Execute();
        }

        public DocumentId DocumentId { get; private set; }

        public MainViewModelBase MainViewModel { get; }
        public ICommandProvider CommandProvider { get; }

        public NuGetDocumentViewModel NuGet { get; }

        public string Title => Document != null && !Document.IsAutoSaveOnly ? Document.Name : "New";

        public IDelegateCommand SaveCommand { get; }

        public IDelegateCommand RunCommand { get; }

        public IDelegateCommand CompileAndSaveCommand { get; }

        public IDelegateCommand RestartHostCommand { get; }

        public IDelegateCommand FormatDocumentCommand { get; }

        public IDelegateCommand CommentSelectionCommand { get; }

        public IDelegateCommand UncommentSelectionCommand { get; }

        public IDelegateCommand RenameSymbolCommand { get; }

        public bool IsRunning
        {
            get => _isRunning; private set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    _dispatcher.InvokeAsync(() => RunCommand.RaiseCanExecuteChanged());
                }
            }
        }

        private async Task CompileAndSave()
        {
            var saveDialog = _serviceProvider.GetService<ISaveFileDialog>();
            saveDialog.OverwritePrompt = true;
            saveDialog.AddExtension = true;
            saveDialog.Filter = new FileDialogFilter("Libraries", "*.dll", "*.exe");
            saveDialog.DefaultExt = "dll";
            var fileName = await saveDialog.ShowAsync().ConfigureAwait(true);
            if (fileName == null) return;

            var code = await GetCode(CancellationToken.None).ConfigureAwait(true);

            var results = new ObservableCollection<IResultObject>();
            ResultsInternal = results;

            HookDumped(results, CancellationToken.None);

            try
            {
                await Task.Run(() => _executionHost.CompileAndSave(code, fileName, OptimizationLevel)).ConfigureAwait(true);
            }
            catch (CompilationErrorException ex)
            {
                foreach (var diagnostic in ex.Diagnostics)
                {
                    results.Add(ResultObject.Create(diagnostic, DumpQuotas.Default));
                }
            }
            catch (Exception ex)
            {
                AddResult(ex, results, CancellationToken.None);
            }
        }

        private async Task Run()
        {
            if (IsRunning) return;

            Reset();

            await MainViewModel.AutoSaveOpenDocuments().ConfigureAwait(true);

            SetIsRunning(true);

            var results = new ObservableCollection<IResultObject>();
            ResultsInternal = results;

            if (!ShowIL)
            {
                ILText = DefaultILText;
            }

            var cancellationToken = _cts.Token;
            HookDumped(results, cancellationToken);
            try
            {
                var code = await GetCode(cancellationToken).ConfigureAwait(true);
                await _executionHost.ExecuteAsync(code, ShowIL, OptimizationLevel).ConfigureAwait(true);
            }
            catch (CompilationErrorException ex)
            {
                foreach (var diagnostic in ex.Diagnostics)
                {
                    results.Add(ResultObject.Create(diagnostic, DumpQuotas.Default));
                }
            }
            catch (Exception ex)
            {
                AddResult(ex, results, cancellationToken);
            }
            finally
            {
                SetIsRunning(false);
            }
        }

        private OptimizationLevel OptimizationLevel => MainViewModel.Settings.OptimizeCompilation ? OptimizationLevel.Release : OptimizationLevel.Debug;

        private async Task UpdatePackages()
        {
            var document = MainViewModel.RoslynHost.GetDocument(DocumentId);
            var syntaxRoot = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            var packages = GetNuGetPackageReferences(syntaxRoot);

            NuGet.UpdatePackageReferences(packages);
        }

        private List<PackageRef> GetNuGetPackageReferences(SyntaxNode syntaxRoot)
        {
            const string NuGetPrefix = "nuget:";
            const string OldNuGetPrefix = "$NuGet\\";

            if (!(syntaxRoot is Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax compilation))
            {
                return null;
            }

            List<PackageRef> packages = null;

            foreach (var directive in compilation.GetReferenceDirectives())
            {
                var value = directive.File.ValueText;
                string id, version;

                if (HasPrefix(NuGetPrefix, value))
                {
                    (id, version) = ParseNuGetReference(NuGetPrefix, value);
                }
                else if (HasPrefix(OldNuGetPrefix, value))
                {
                    (id, version) = GetOldNuGetReference(value);
                    if (id == null)
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }

                VersionRange versionRange;
                if (version == string.Empty)
                {
                    versionRange = VersionRange.All;
                }
                else if (!VersionRange.TryParse(version, out versionRange))
                {
                    continue;
                }

                if (packages == null) packages = new List<PackageRef>();
                packages.Add(new PackageRef(id, versionRange));
            }

            return packages;
        }

        private static (string id, string version) GetOldNuGetReference(string value)
        {
            var split = value.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length >= 3)
            {
                return (split[1], split[2]);
            }

            return (null, null);
        }

        private static (string id, string version) ParseNuGetReference(string prefix, string value)
        {
            string id, version;

            var indexOfSlash = value.IndexOf('/');
            if (indexOfSlash >= 0)
            {
                id = value.Substring(prefix.Length, indexOfSlash - prefix.Length);
                version = indexOfSlash != value.Length - 1 ? value.Substring(indexOfSlash + 1) : string.Empty;
            }
            else
            {
                id = value.Substring(prefix.Length);
                version = string.Empty;
            }

            return (id, version);
        }

        private static bool HasPrefix(string prefix, string value)
        {
            return value.Length > prefix.Length &&
                   value.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase);
        }

        private void HookDumped(ObservableCollection<IResultObject> results, CancellationToken cancellationToken)
        {
            _onError?.Invoke(null);

            if (_executionHostOnDumped != null)
            {
                _executionHost.Dumped -= _executionHostOnDumped;
            }

            _executionHostOnDumped = o =>
            {
                AddResult(o, results, cancellationToken);
                ResultsAvailable?.Invoke();
            };

            _executionHost.Dumped += _executionHostOnDumped;
        }

        private async Task<string> GetCode(CancellationToken cancellationToken)
        {
            return (await MainViewModel.RoslynHost.GetDocument(DocumentId).GetTextAsync(cancellationToken)
                .ConfigureAwait(false)).ToString();
        }

        private void Reset()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            _cts = new CancellationTokenSource();
        }

        private void AddResult(object o, ObservableCollection<IResultObject> results, CancellationToken cancellationToken)
        {
            _dispatcher.InvokeAsync(() =>
            {
                if (o is IEnumerable<ResultObject> list)
                {
                    foreach (var resultObject in list)
                    {
                        results.Add(resultObject);
                    }
                }
                else
                {
                    results.Add(ResultObject.Create(o, DumpQuotas.Default));
                }
            }, AppDispatcherPriority.Low, cancellationToken);
        }

        public async Task<string> LoadText()
        {
            if (Document == null)
            {
                return string.Empty;
            }
            using (var fileStream = File.OpenText(Document.Path))
            {
                return await fileStream.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        public void Close()
        {
            _viewDisposable?.Dispose();
            _executionHost?.Dispose();
            _executionHost = null;
        }

        public bool IsDirty
        {
            get => _isDirty;
            private set => SetProperty(ref _isDirty, value);
        }

        public bool ShowIL { get; set; }

        public event EventHandler EditorFocus;

        private void OnEditorFocus()
        {
            EditorFocus?.Invoke(this, EventArgs.Empty);
        }

        public void OnTextChanged()
        {
            IsDirty = true;

            if (IsLiveMode)
            {
                _liveModeTimer?.Change(MainViewModel.Settings.LiveModeDelayMs, Timeout.Infinite);
            }

            var task = UpdatePackages();
        }
    }
}