using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace RoslynPad.Roslyn.QuickInfo
{
    [Export(typeof(IQuickInfoProvider))]
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    internal sealed class QuickInfoProvider : IQuickInfoProvider
    {
        public async Task<QuickInfoItem> GetItemAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = await tree.GetTouchingTokenAsync(position, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);

            var state = await GetQuickInfoItemAsync(document, token, position, cancellationToken).ConfigureAwait(false);
            if (state != null)
            {
                return state;
            }

            if (ShouldCheckPreviousToken(token))
            {
                var previousToken = token.GetPreviousToken();

                if ((state = await GetQuickInfoItemAsync(document, previousToken, position, cancellationToken).ConfigureAwait(false)) != null)
                {
                    return state;
                }
            }

            return null;
        }

        private static bool ShouldCheckPreviousToken(SyntaxToken token)
        {
            return !token.Parent.IsKind(SyntaxKind.XmlCrefAttribute);
        }

        private async Task<QuickInfoItem> GetQuickInfoItemAsync(
            Document document,
            SyntaxToken token,
            int position,
            CancellationToken cancellationToken)
        {
            if (token != default(SyntaxToken) &&
                token.Span.IntersectsWith(position))
            {
                var deferredContent = await BuildContentAsync(document, token, cancellationToken).ConfigureAwait(false);
                if (deferredContent != null)
                {
                    return new QuickInfoItem(token.Span, () => deferredContent.Create());
                }
            }

            return null;
        }

        private async Task<IDeferredQuickInfoContent> BuildContentAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var linkedDocumentIds = document.GetLinkedDocumentIds();

            var modelAndSymbols = await BindTokenAsync(document, token, cancellationToken).ConfigureAwait(false);
            if ((modelAndSymbols.Item2 == null || modelAndSymbols.Item2.Count == 0) && !linkedDocumentIds.Any())
            {
                return null;
            }

            if (!linkedDocumentIds.Any())
            {
                return await CreateContentAsync(document.Project.Solution.Workspace,
                    token,
                    modelAndSymbols.Item1,
                    modelAndSymbols.Item2,
                    supportedPlatforms: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // Linked files/shared projects: imagine the following when FOO is false
            // #if FOO
            // int x = 3;
            // #endif 
            // var y = x$$;
            //
            // 'x' will bind as an error type, so we'll show incorrect information.
            // Instead, we need to find the head in which we get the best binding, 
            // which in this case is the one with no errors.

            var candidateProjects = new List<ProjectId> { document.Project.Id };
            var invalidProjects = new List<ProjectId>();

            var candidateResults = new List<Tuple<DocumentId, SemanticModel, IList<ISymbol>>>();
            candidateResults.Add(Tuple.Create(document.Id, modelAndSymbols.Item1, modelAndSymbols.Item2));

            foreach (var link in linkedDocumentIds)
            {
                var linkedDocument = document.Project.Solution.GetDocument(link);
                var linkedToken = await FindTokenInLinkedDocument(token, linkedDocument, cancellationToken).ConfigureAwait(false);

                if (linkedToken != default(SyntaxToken))
                {
                    // Not in an inactive region, so this file is a candidate.
                    candidateProjects.Add(link.ProjectId);
                    var linkedModelAndSymbols = await BindTokenAsync(linkedDocument, linkedToken, cancellationToken).ConfigureAwait(false);
                    candidateResults.Add(Tuple.Create(link, linkedModelAndSymbols.Item1, linkedModelAndSymbols.Item2));
                }
            }

            // Take the first result with no errors.
            var bestBinding = candidateResults.FirstOrDefault(c => c.Item3.Count > 0 && !ErrorVisitor.ContainsError(c.Item3.FirstOrDefault()));

            // Every file binds with errors. Take the first candidate, which is from the current file.
            if (bestBinding == null)
            {
                bestBinding = candidateResults.First();
            }

            if (bestBinding.Item3 == null || !bestBinding.Item3.Any())
            {
                return null;
            }

            // We calculate the set of supported projects
            candidateResults.Remove(bestBinding);
            foreach (var candidate in candidateResults)
            {
                // Does the candidate have anything remotely equivalent?
                if (!candidate.Item3.Intersect(bestBinding.Item3, LinkedFilesSymbolEquivalenceComparer.Instance).Any())
                {
                    invalidProjects.Add(candidate.Item1.ProjectId);
                }
            }

            var supportedPlatforms = new SupportedPlatformData(invalidProjects, candidateProjects, document.Project.Solution.Workspace);
            return await CreateContentAsync(document.Project.Solution.Workspace, token, bestBinding.Item2, bestBinding.Item3, supportedPlatforms, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<SyntaxToken> FindTokenInLinkedDocument(SyntaxToken token, Document linkedDocument, CancellationToken cancellationToken)
        {
            if (!linkedDocument.SupportsSyntaxTree)
            {
                return default(SyntaxToken);
            }

            var root = await linkedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Don't search trivia because we want to ignore inactive regions
            var linkedToken = root.FindToken(token.SpanStart);

            // The new and old tokens should have the same span?
            if (token.Span == linkedToken.Span)
            {
                return linkedToken;
            }

            return default(SyntaxToken);
        }

        private async Task<IDeferredQuickInfoContent> CreateContentAsync(
            Workspace workspace,
            SyntaxToken token,
            SemanticModel semanticModel,
            IEnumerable<ISymbol> symbols,
            SupportedPlatformData supportedPlatforms,
            CancellationToken cancellationToken)
        {
            var descriptionService = workspace.Services.GetLanguageServices(token.Language).GetService<ISymbolDisplayService>();

            var sections = await descriptionService.ToDescriptionGroupsAsync(workspace, semanticModel, token.SpanStart, symbols.AsImmutable(), cancellationToken).ConfigureAwait(false);

            var mainDescriptionBuilder = new List<TaggedText>();
            if (sections.ContainsKey(SymbolDescriptionGroups.MainDescription))
            {
                mainDescriptionBuilder.AddRange(sections[SymbolDescriptionGroups.MainDescription]);
            }

            var typeParameterMapBuilder = new List<TaggedText>();
            if (sections.ContainsKey(SymbolDescriptionGroups.TypeParameterMap))
            {
                var parts = sections[SymbolDescriptionGroups.TypeParameterMap];
                if (!parts.IsDefaultOrEmpty)
                {
                    typeParameterMapBuilder.AddLineBreak();
                    typeParameterMapBuilder.AddRange(parts);
                }
            }

            var anonymousTypesBuilder = new List<TaggedText>();
            if (sections.ContainsKey(SymbolDescriptionGroups.AnonymousTypes))
            {
                var parts = sections[SymbolDescriptionGroups.AnonymousTypes];
                if (!parts.IsDefaultOrEmpty)
                {
                    anonymousTypesBuilder.AddLineBreak();
                    anonymousTypesBuilder.AddRange(parts);
                }
            }

            var usageTextBuilder = new List<TaggedText>();
            if (sections.ContainsKey(SymbolDescriptionGroups.AwaitableUsageText))
            {
                var parts = sections[SymbolDescriptionGroups.AwaitableUsageText];
                if (!parts.IsDefaultOrEmpty)
                {
                    usageTextBuilder.AddRange(parts);
                }
            }

            if (supportedPlatforms != null)
            {
                usageTextBuilder.AddRange(supportedPlatforms.ToDisplayParts().ToTaggedText());
            }

            var exceptionsTextBuilder = new List<TaggedText>();
            if (sections.ContainsKey(SymbolDescriptionGroups.Exceptions))
            {
                var parts = sections[SymbolDescriptionGroups.Exceptions];
                if (!parts.IsDefaultOrEmpty)
                {
                    exceptionsTextBuilder.AddRange(parts);
                }
            }

            var formatter = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<IDocumentationCommentFormattingService>();
            var syntaxFactsService = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<ISyntaxFactsService>();
            var documentationContent = GetDocumentationContent(symbols, sections, semanticModel, token, formatter, syntaxFactsService, cancellationToken);
            var showWarningGlyph = supportedPlatforms != null && supportedPlatforms.HasValidAndInvalidProjects();
            var showSymbolGlyph = true;

            if (workspace.Services.GetLanguageServices(semanticModel.Language).GetService<ISyntaxFactsService>().IsAwaitKeyword(token) &&
                (symbols.First() as INamedTypeSymbol)?.SpecialType == SpecialType.System_Void)
            {
                documentationContent = CreateDocumentationCommentDeferredContent(null);
                showSymbolGlyph = false;
            }

            return CreateQuickInfoDisplayDeferredContent(
                symbol: symbols.First(),
                showWarningGlyph: showWarningGlyph,
                showSymbolGlyph: showSymbolGlyph,
                mainDescription: mainDescriptionBuilder,
                documentation: documentationContent,
                typeParameterMap: typeParameterMapBuilder,
                anonymousTypes: anonymousTypesBuilder,
                usageText: usageTextBuilder,
                exceptionText: exceptionsTextBuilder);
        }

        private IDeferredQuickInfoContent CreateQuickInfoDisplayDeferredContent(
            ISymbol symbol,
            bool showWarningGlyph,
            bool showSymbolGlyph,
            IList<TaggedText> mainDescription,
            IDeferredQuickInfoContent documentation,
            IList<TaggedText> typeParameterMap,
            IList<TaggedText> anonymousTypes,
            IList<TaggedText> usageText,
            IList<TaggedText> exceptionText)
        {
            return new QuickInfoDisplayDeferredContent(
                symbolGlyph: showSymbolGlyph ? CreateGlyphDeferredContent(symbol) : null,
                warningGlyph: showWarningGlyph ? CreateWarningGlyph() : null,
                mainDescription: CreateClassifiableDeferredContent(mainDescription),
                documentation: documentation,
                typeParameterMap: CreateClassifiableDeferredContent(typeParameterMap),
                anonymousTypes: CreateClassifiableDeferredContent(anonymousTypes),
                usageText: CreateClassifiableDeferredContent(usageText),
                exceptionText: CreateClassifiableDeferredContent(exceptionText));
        }

        private static IDeferredQuickInfoContent CreateGlyphDeferredContent(ISymbol symbol)
        {
            return new SymbolGlyphDeferredContent((Glyph)symbol.GetGlyph());
        }

        private static IDeferredQuickInfoContent CreateWarningGlyph()
        {
            return new SymbolGlyphDeferredContent(Glyph.CompletionWarning);
        }

        private IDeferredQuickInfoContent CreateDocumentationCommentDeferredContent(string documentationComment)
        {
            return new DocumentationCommentDeferredContent(documentationComment);
        }

        private static IDeferredQuickInfoContent CreateClassifiableDeferredContent(IList<TaggedText> content)
        {
            return new ClassifiableDeferredContent(content);
        }

        private class SymbolGlyphDeferredContent : IDeferredQuickInfoContent
        {
            public SymbolGlyphDeferredContent(Glyph glyph)
            {
                Glyph = glyph;
            }

            public FrameworkElement Create()
            {
                var image = new Image
                {
                    Width = 16,
                    Height = 16,
                    Source = Application.Current?.TryFindResource((Completion.Glyph)Glyph) as ImageSource
                };
                return image;
            }

            private Glyph Glyph { get; }
        }

        private class ClassifiableDeferredContent : IDeferredQuickInfoContent
        {
            private readonly IList<TaggedText> _classifiableContent;

            public ClassifiableDeferredContent(IList<TaggedText> content)
            {
                _classifiableContent = content;
            }

            public FrameworkElement Create()
            {
                var textBlock = _classifiableContent.ToTextBlock();
                if (textBlock.Inlines.Count == 0)
                    textBlock.Visibility = Visibility.Collapsed;
                return textBlock;
            }
        }

        private class DocumentationCommentDeferredContent : IDeferredQuickInfoContent
        {
            private readonly string _documentationComment;

            public DocumentationCommentDeferredContent(string documentationComment)
            {
                _documentationComment = documentationComment;
            }

            public FrameworkElement Create()
            {
                var documentationTextBlock = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap
                };

                UpdateDocumentationTextBlock(documentationTextBlock);
                return documentationTextBlock;
            }

            private void UpdateDocumentationTextBlock(TextBlock documentationTextBlock)
            {
                if (!string.IsNullOrEmpty(_documentationComment))
                {
                    documentationTextBlock.Text = _documentationComment;
                }
                else
                {
                    documentationTextBlock.Text = string.Empty;
                    documentationTextBlock.Visibility = Visibility.Collapsed;
                }
            }
        }

        private IDeferredQuickInfoContent GetDocumentationContent(
            IEnumerable<ISymbol> symbols,
            IDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>> sections,
            SemanticModel semanticModel,
            SyntaxToken token,
            IDocumentationCommentFormattingService formatter,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            if (sections.ContainsKey(SymbolDescriptionGroups.Documentation))
            {
                var documentationBuilder = new List<TaggedText>();
                documentationBuilder.AddRange(sections[SymbolDescriptionGroups.Documentation]);
                return CreateClassifiableDeferredContent(documentationBuilder);
            }
            if (symbols.Any())
            {
                var symbol = symbols.First().OriginalDefinition;

                // if generating quick info for an attribute, bind to the class instead of the constructor
                if (syntaxFactsService.IsAttributeName(token.Parent) &&
                    symbol.ContainingType?.IsAttribute() == true)
                {
                    symbol = symbol.ContainingType;
                }

                var documentation = symbol.GetDocumentationParts(semanticModel, token.SpanStart, formatter, cancellationToken);

                if (documentation != null)
                {
                    return CreateClassifiableDeferredContent(documentation.ToList());
                }
            }

            return CreateDocumentationCommentDeferredContent(null);
        }

        private async Task<ValueTuple<SemanticModel, IList<ISymbol>>> BindTokenAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelForNodeAsync(token.Parent, cancellationToken).ConfigureAwait(false);
            var enclosingType = semanticModel.GetEnclosingNamedType(token.SpanStart, cancellationToken);

            var symbols = semanticModel.GetSymbols(token, document.Project.Solution.Workspace, bindLiteralsToUnderlyingType: true, cancellationToken: cancellationToken);

            var bindableParent = document.GetLanguageService<ISyntaxFactsService>().GetBindableParent(token);
            var overloads = semanticModel.GetMemberGroup(bindableParent, cancellationToken);

            symbols = symbols.Where(IsOk)
                .Where(s => IsAccessible(s, enclosingType))
                .Concat(overloads)
                .Distinct(SymbolEquivalenceComparer.Instance);

            if (symbols.Any())
            {
                var typeParameter = symbols.First() as ITypeParameterSymbol;
                return new ValueTuple<SemanticModel, IList<ISymbol>>(
                    semanticModel,
                    typeParameter != null && typeParameter.TypeParameterKind == TypeParameterKind.Cref
                        ? SpecializedCollections.EmptyList<ISymbol>()
                        : symbols.ToList());
            }

            // Couldn't bind the token to specific symbols.  If it's an operator, see if we can at
            // least bind it to a type.
            var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            if (syntaxFacts.IsOperator(token))
            {
                var typeInfo = semanticModel.GetTypeInfo(token.Parent, cancellationToken);
                if (IsOk(typeInfo.Type))
                {
                    return new ValueTuple<SemanticModel, IList<ISymbol>>(semanticModel, new List<ISymbol>(1) { typeInfo.Type });
                }
            }

            return ValueTuple.Create(semanticModel, SpecializedCollections.EmptyList<ISymbol>());
        }

        private static bool IsOk(ISymbol symbol)
        {
            return symbol != null && !symbol.IsErrorType() && !symbol.IsAnonymousFunction();
        }

        private static bool IsAccessible(ISymbol symbol, INamedTypeSymbol within)
        {
            return within == null || symbol.IsAccessibleWithin(within);
        }

        private class ErrorVisitor : SymbolVisitor<bool>
        {
            private static readonly ErrorVisitor _instance = new ErrorVisitor();

            public static bool ContainsError(ISymbol symbol)
            {
                return _instance.Visit(symbol);
            }

            public override bool DefaultVisit(ISymbol symbol)
            {
                return true;
            }

            public override bool VisitAlias(IAliasSymbol symbol)
            {
                return false;
            }

            public override bool VisitArrayType(IArrayTypeSymbol symbol)
            {
                return Visit(symbol.ElementType);
            }

            public override bool VisitEvent(IEventSymbol symbol)
            {
                return Visit(symbol.Type);
            }

            public override bool VisitField(IFieldSymbol symbol)
            {
                return Visit(symbol.Type);
            }

            public override bool VisitLocal(ILocalSymbol symbol)
            {
                return Visit(symbol.Type);
            }

            public override bool VisitMethod(IMethodSymbol symbol)
            {
                foreach (var parameter in symbol.Parameters)
                {
                    if (!Visit(parameter))
                    {
                        return true;
                    }
                }

                foreach (var typeParameter in symbol.TypeParameters)
                {
                    if (!Visit(typeParameter))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override bool VisitNamedType(INamedTypeSymbol symbol)
            {
                foreach (var typeParameter in symbol.TypeArguments.Concat(symbol.TypeParameters))
                {
                    if (Visit(typeParameter))
                    {
                        return true;
                    }
                }

                return symbol.IsErrorType();
            }

            public override bool VisitParameter(IParameterSymbol symbol)
            {
                return Visit(symbol.Type);
            }

            public override bool VisitProperty(IPropertySymbol symbol)
            {
                return Visit(symbol.Type);
            }

            public override bool VisitPointerType(IPointerTypeSymbol symbol)
            {
                return Visit(symbol.PointedAtType);
            }
        }
    }
}