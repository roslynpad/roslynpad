using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.CSharp;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Runtime;

namespace RoslynPad.Roslyn
{
    public class InteractiveManager
    {
        #region Fields

        private static readonly Type[] _assemblyTypes =
        {
            typeof (object),
            typeof (Task),
            typeof (List<>),
            typeof (Regex),
            typeof (StringBuilder),
            typeof (Uri),
            typeof (Enumerable),
            typeof (ObjectExtensions)
        };

        private readonly InteractiveWorkspace _workspace;
        private readonly CSharpParseOptions _parseOptions;
        private readonly CSharpCompilationOptions _compilationOptions;
        private readonly MetadataReference[] _references;
        private readonly ISignatureHelpProvider[] _signatureHelpProviders;

        private int _documentNumber;
        private DocumentId _currentDocumenId;

        #endregion

        public Solution Solution
        {
            get { return _workspace.CurrentSolution; }
        }

        public InteractiveManager()
        {
            var host = MefHostServices.Create(MefHostServices.DefaultAssemblies.Concat(new[]
            {
                Assembly.Load("Microsoft.CodeAnalysis.Features"),
                Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features"),
            }));
            _workspace = new InteractiveWorkspace(host);
            _parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Interactive);

            _references = _assemblyTypes.Select(t =>
                MetadataReference.CreateFromAssembly(t.Assembly)).ToArray();
            _compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                usings: _assemblyTypes.Select(x => x.Namespace).ToImmutableArray());

            var container = new CompositionContainer(new AssemblyCatalog(typeof(ISignatureHelpProvider).Assembly),
                CompositionOptions.DisableSilentRejection | CompositionOptions.IsThreadSafe);
            _signatureHelpProviders = container.GetExportedValues<ISignatureHelpProvider>().ToArray();
        }

        #region Documents

        public void SetDocument(SourceTextContainer textContainer)
        {
            var currentSolution = _workspace.CurrentSolution;
            var project = CreateSubmissionProject(currentSolution);
            var currentDocument = SetSubmissionDocument(textContainer, project);
            _currentDocumenId = currentDocument.Id;
        }

        private Document SetSubmissionDocument(SourceTextContainer textContainer, Project project)
        {
            var id = DocumentId.CreateNewId(project.Id);
            var solution = project.Solution.AddDocument(id, project.Name, textContainer.CurrentText);
            _workspace.SetCurrentSolution(solution);
            _workspace.OpenDocument(id, textContainer);
            return solution.GetDocument(id);
        }

        private Project CreateSubmissionProject(Solution solution)
        {
            string name = "Program" + _documentNumber++;
            ProjectId id = ProjectId.CreateNewId(name);
            solution = solution.AddProject(ProjectInfo.Create(id, VersionStamp.Create(), name, name, LanguageNames.CSharp,
                parseOptions: _parseOptions,
                compilationOptions: _compilationOptions.WithScriptClassName(name),
                metadataReferences: _references));
            //if (_previousProjectId != null)
            //{
            //    solution = solution.AddProjectReference(id, new ProjectReference(_previousProjectId));
            //}
            return solution.GetProject(id);
        }

        #endregion

        #region Completion

        public async Task<IList<CompletionItem>> GetCompletion(CompletionTriggerInfo trigger, int position)
        {
            var groups = await CompletionService.GetCompletionItemGroupsAsync(
                GetCurrentDocument(), position, trigger).ConfigureAwait(false);
            if (groups == null)
            {
                return new CompletionItem[0];
            }
            return groups.SelectMany(t => t.Items).ToArray();
        }

        private Document GetCurrentDocument()
        {
            return _workspace.CurrentSolution.GetDocument(_currentDocumenId);
        }

        public Task<bool> IsCompletionTriggerCharacter(int position)
        {
            return CompletionService.IsCompletionTriggerCharacterAsync(GetCurrentDocument(), position);
        }

        #endregion

        #region Signature Help

        public async Task<bool> IsSignatureHelpTriggerCharacter(int position)
        {
            var text = await GetCurrentDocument().GetTextAsync().ConfigureAwait(false);
            var character = text.GetSubText(new TextSpan(position, 1))[0];
            return _signatureHelpProviders.Any(p => p.IsTriggerCharacter(character));
        }

        public async Task<SignatureHelpItems> GetSignatureHelp(SignatureHelpTriggerInfo trigger, int position)
        {
            var document = GetCurrentDocument();
            foreach (var provider in _signatureHelpProviders)
            {
                var items = await provider.GetItemsAsync(document, position, trigger, CancellationToken.None)
                            .ConfigureAwait(false);
                if (items != null)
                {
                    return items;
                }
            }
            return null;
        }

        #endregion

        public void Execute()
        {
            SourceText text;
            if (GetCurrentDocument().TryGetText(out text))
            {
                CSharpScript.Run(text.ToString(),
                    ScriptOptions.Default
                        .AddNamespaces(_assemblyTypes.Select(x => x.Namespace))
                        .AddReferences(_assemblyTypes.Select(x => x.Assembly)));
            }
        }
    }
}
