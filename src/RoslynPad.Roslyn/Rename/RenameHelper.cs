using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace RoslynPad.Roslyn.Rename
{
    public static class RenameHelper
    {
        public static async Task<ISymbol?> GetRenameSymbol(
            Document document, int position, CancellationToken cancellationToken = default)
        {
            var token = await document.GetTouchingWordAsync(position, cancellationToken).ConfigureAwait(false);
            return token != default 
                    ? await GetRenameSymbol(document, token, cancellationToken).ConfigureAwait(false)
                    : null;
        }

        public static async Task<ISymbol?> GetRenameSymbol(
            Document document, SyntaxToken triggerToken, CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.Project.Services.GetRequiredService<ISyntaxFactsService>();
            if (syntaxFactsService.IsReservedOrContextualKeyword(triggerToken))
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return null;
            }

            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            var tokenRenameInfo = RenameUtilities.GetTokenRenameInfo(semanticFacts, semanticModel, triggerToken, cancellationToken);

            // Rename was invoked on a member group reference in a nameof expression.
            // Trigger the rename on any of the candidate symbols but force the 
            // RenameOverloads option to be on.
            var triggerSymbol = tokenRenameInfo.HasSymbols ? tokenRenameInfo.Symbols.First() : null;
            if (triggerSymbol == null)
            {
                return null;
            }

            // see https://github.com/dotnet/roslyn/issues/10898
            // we are disabling rename for tuple fields for now
            // 1) compiler does not return correct location information in these symbols
            // 2) renaming tuple fields seems a complex enough thing to require some design
            if (triggerSymbol.ContainingType?.IsTupleType == true)
            {
                return null;
            }

            // If rename is invoked on a member group reference in a nameof expression, then the
            // RenameOverloads option should be forced on.
            var forceRenameOverloads = tokenRenameInfo.IsMemberGroup;

            var symbol = await RenameUtilities.TryGetRenamableSymbolAsync(document, triggerToken.SpanStart, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (symbol == null)
            {
                return null;
            }

            if (symbol.Kind == SymbolKind.Alias && symbol.IsExtern ||
                triggerToken.IsTypeNamedDynamic() && symbol.Kind == SymbolKind.DynamicType)
            {
                return null;
            }

            // we allow implicit locals and parameters of Event handlers
            if (symbol.IsImplicitlyDeclared &&
                symbol.Kind != SymbolKind.Local &&
                !(symbol.Kind == SymbolKind.Parameter &&
                  symbol.ContainingSymbol.Kind == SymbolKind.Method &&
                  symbol.ContainingType != null &&
                  symbol.ContainingType.IsDelegateType() &&
                  symbol.ContainingType.AssociatedSymbol != null))
            {
                // We enable the parameter in RaiseEvent, if the Event is declared with a signature. If the Event is declared as a 
                // delegate type, we do not have a connection between the delegate type and the event.
                // this prevents a rename in this case :(.
                return null;
            }

            if (symbol.Kind == SymbolKind.Property && symbol.ContainingType.IsAnonymousType)
            {
                return null;
            }

            if (symbol.IsErrorType())
            {
                return null;
            }

            if (symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).MethodKind == MethodKind.UserDefinedOperator)
            {
                return null;
            }

            var symbolLocations = symbol.Locations;

            // Does our symbol exist in an unchangeable location?
            foreach (var location in symbolLocations)
            {
                if (location.IsInMetadata)
                {
                    return null;
                }
                if (location.IsInSource)
                {
                    if (document.Project.IsSubmission)
                    {
                        var solution = document.Project.Solution;
                        var projectIdOfLocation = solution.GetDocument(location.SourceTree)?.Project.Id;

                        if (solution.Projects.Any(p => p.IsSubmission && p.ProjectReferences.Any(r => r.ProjectId == projectIdOfLocation)))
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }

            return symbol;
        }
    }
}
