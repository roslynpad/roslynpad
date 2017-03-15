using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Completion.Providers
{
    internal abstract class AbstractReferenceDirectiveCompletionProvider : CommonCompletionProvider
    {
        private static readonly ImmutableArray<CharacterSetModificationRule> _commitRules =
            ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, '"', '\\', ','));

        private static readonly ImmutableArray<CharacterSetModificationRule> _filterRules = 
            ImmutableArray<CharacterSetModificationRule>.Empty;

        private static readonly CompletionItemRules _rules = CompletionItemRules.Create(
            filterCharacterRules: _filterRules, commitCharacterRules: _commitRules, enterKeyRule: EnterKeyRule.Never);

        protected abstract bool TryGetStringLiteralToken(SyntaxTree tree, int position, out SyntaxToken stringLiteral, CancellationToken cancellationToken);

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return PathCompletionUtilities.IsTriggerCharacter(text, characterPosition);
        }

        private static TextSpan GetTextChangeSpan(SyntaxToken stringLiteral, int position)
        {
            return PathCompletionUtilities.GetTextChangeSpan(
                quotedPath: stringLiteral.ToString(),
                quotedPathStart: stringLiteral.SpanStart,
                position: position);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            // first try to get the #r string literal token.  If we couldn't, then we're not in a #r
            // reference directive and we immediately bail.
            SyntaxToken stringLiteral;
            if (!TryGetStringLiteralToken(tree, position, out stringLiteral, cancellationToken))
            {
                return;
            }

            var textChangeSpan = GetTextChangeSpan(stringLiteral, position);

            var gacHelper = new GlobalAssemblyCacheCompletionHelper(_rules);
            var referenceResolver = document.Project.CompilationOptions.MetadataReferenceResolver;

            // TODO: https://github.com/dotnet/roslyn/issues/5263
            // Avoid dependency on a specific resolvers.
            // The search paths should be provided by specialized workspaces:
            // - InteractiveWorkspace for interactive window 
            // - ScriptWorkspace for loose .csx files (we don't have such workspace today)
            ImmutableArray<string> searchPaths;

            RuntimeMetadataReferenceResolver rtResolver;
            WorkspaceMetadataFileReferenceResolver workspaceResolver;

            if ((rtResolver = referenceResolver as RuntimeMetadataReferenceResolver) != null)
            {
                searchPaths = rtResolver.PathResolver.SearchPaths;
            }
            else if ((workspaceResolver = referenceResolver as WorkspaceMetadataFileReferenceResolver) != null)
            {
                searchPaths = workspaceResolver.PathResolver.SearchPaths;
            }
            else
            {
                return;
            }

            var fileSystemHelper = new FileSystemCompletionHelper(Microsoft.CodeAnalysis.Glyph.OpenFolder,
                Microsoft.CodeAnalysis.Glyph.Assembly, searchPaths, new[] { ".dll", ".exe" }, path => path.Contains(","), _rules);

            var pathThroughLastSlash = GetPathThroughLastSlash(stringLiteral, position);

            var documentPath = document.Project.IsSubmission ? null : document.FilePath;
            context.AddItems(gacHelper.GetItems(pathThroughLastSlash, documentPath));
            context.AddItems(fileSystemHelper.GetItems(pathThroughLastSlash, documentPath));
        }


        private static string GetPathThroughLastSlash(SyntaxToken stringLiteral, int position)
        {
            return PathCompletionUtilities.GetPathThroughLastSlash(
                quotedPath: stringLiteral.ToString(),
                quotedPathStart: stringLiteral.SpanStart,
                position: position);
        }
    }
}