using System.Collections;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeDefinitionWindow;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Morgania.CodeAnalysis.Editor;

// Host services Roslyn EditorFeatures imports that Visual Studio normally provides. Without
// them VS-MEF rejects the importing parts — including the completion and quick info sources —
// so even features the demo does not surface need at least a no-op implementation.

/// <summary>No-op find-usages presenter; unblocks completion/quick info/go-to services.</summary>
[Shared]
[Export(typeof(IStreamingFindUsagesPresenter))]
internal sealed class NoOpStreamingFindUsagesPresenter : IStreamingFindUsagesPresenter
{
    public (FindUsagesContext context, CancellationToken cancellationToken) StartSearch(string title, StreamingFindUsagesPresenterOptions options) =>
        (new NoOpFindUsagesContext(), CancellationToken.None);

    public void ClearAll()
    {
    }

    private sealed class NoOpFindUsagesContext : FindUsagesContext;
}

/// <summary>Maps formatting queries to the buffer's editor options; unblocks EditorOptionsService.</summary>
[Shared]
[Export(typeof(IIndentationManagerService))]
public sealed class IndentationManagerService : IIndentationManagerService
{
    private readonly IEditorOptionsFactoryService _optionsFactory;

    [ImportingConstructor]
    public IndentationManagerService(IEditorOptionsFactoryService optionsFactory)
    {
        _optionsFactory = optionsFactory;
    }

    public void GetIndentation(ITextBuffer buffer, bool explicitFormat, out bool convertTabsToSpaces, out int tabSize, out int indentSize)
    {
        var options = _optionsFactory.GetOptions(buffer);
        convertTabsToSpaces = options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId);
        tabSize = options.GetOptionValue(DefaultOptions.TabSizeOptionId);
        indentSize = options.GetOptionValue(DefaultOptions.IndentSizeOptionId);
    }

    public bool UseSpacesForWhitespace(ITextBuffer buffer, bool explicitFormat) =>
        _optionsFactory.GetOptions(buffer).GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId);

    public int GetTabSize(ITextBuffer buffer, bool explicitFormat) =>
        _optionsFactory.GetOptions(buffer).GetOptionValue(DefaultOptions.TabSizeOptionId);

    public int GetIndentSize(ITextBuffer buffer, bool explicitFormat) =>
        _optionsFactory.GetOptions(buffer).GetOptionValue(DefaultOptions.IndentSizeOptionId);
}

/// <summary>
/// The suggested-action category registry the VS shell normally provides; unblocks Roslyn's
/// suggested-actions (light bulb) source.
/// </summary>
[Shared]
[Export(typeof(ISuggestedActionCategoryRegistryService))]
public sealed class SuggestedActionCategoryRegistryService : ISuggestedActionCategoryRegistryService
{
    private readonly ImmutableDictionary<string, ISuggestedActionCategory> _categories;

    public SuggestedActionCategoryRegistryService()
    {
        var any = new Category(PredefinedSuggestedActionCategoryNames.Any, []);
        var refactoring = new Category(PredefinedSuggestedActionCategoryNames.Refactoring, [any]);
        var codeFix = new Category(PredefinedSuggestedActionCategoryNames.CodeFix, [any]);
        var errorFix = new Category(PredefinedSuggestedActionCategoryNames.ErrorFix, [codeFix]);
        var styleFix = new Category(PredefinedSuggestedActionCategoryNames.StyleFix, [codeFix]);
        _categories = new ISuggestedActionCategory[] { any, refactoring, codeFix, errorFix, styleFix }
            .ToImmutableDictionary(category => category.CategoryName, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<ISuggestedActionCategory> Categories => _categories.Values;

    public ISuggestedActionCategorySet Any { get; } = new CategorySet([PredefinedSuggestedActionCategoryNames.Any]);

    public ISuggestedActionCategorySet AllCodeFixes { get; } = new CategorySet(
        [PredefinedSuggestedActionCategoryNames.CodeFix, PredefinedSuggestedActionCategoryNames.ErrorFix, PredefinedSuggestedActionCategoryNames.StyleFix]);

    public ISuggestedActionCategorySet AllRefactorings { get; } = new CategorySet([PredefinedSuggestedActionCategoryNames.Refactoring]);

    public ISuggestedActionCategorySet AllCodeFixesAndRefactorings { get; } = new CategorySet(
        [PredefinedSuggestedActionCategoryNames.CodeFix, PredefinedSuggestedActionCategoryNames.ErrorFix, PredefinedSuggestedActionCategoryNames.StyleFix, PredefinedSuggestedActionCategoryNames.Refactoring]);

    public ISuggestedActionCategory GetCategory(string categoryName) =>
        _categories.TryGetValue(categoryName, out var category) ? category : null!;

    public ISuggestedActionCategorySet CreateSuggestedActionCategorySet(IEnumerable<string> categories) =>
        new CategorySet([.. categories]);

    public ISuggestedActionCategorySet CreateSuggestedActionCategorySet(params string[] categories) =>
        new CategorySet([.. categories]);

    private sealed class Category(string name, ISuggestedActionCategory[] baseCategories) : ISuggestedActionCategory
    {
        public string CategoryName => name;

        public string DisplayName => name;

        public IEnumerable<ISuggestedActionCategory> BaseCategories => baseCategories;

        public bool IsOfCategory(string category) =>
            string.Equals(name, category, StringComparison.OrdinalIgnoreCase) ||
            baseCategories.Any(baseCategory => baseCategory.IsOfCategory(category));
    }

    private sealed class CategorySet(ImmutableArray<string> categories) : ISuggestedActionCategorySet
    {
        public bool Contains(string categoryName) => categories.Contains(categoryName, StringComparer.OrdinalIgnoreCase);

        public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)categories).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

/// <summary>No previews (code actions apply without a diff preview); unblocks CodeActionEditHandlerService.</summary>
[Shared]
[Export(typeof(IPreviewFactoryService))]
internal sealed class NoOpPreviewFactoryService : IPreviewFactoryService
{
    public SolutionPreviewResult? GetSolutionPreviews(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken) => null;

    public SolutionPreviewResult? GetSolutionPreviews(Solution oldSolution, Solution newSolution, double zoomLevel, CancellationToken cancellationToken) => null;
}

/// <summary>There is no code definition window outside VS.</summary>
[Shared]
[Export(typeof(ICodeDefinitionWindowService))]
internal sealed class NoOpCodeDefinitionWindowService : ICodeDefinitionWindowService
{
    public Task<bool> IsWindowOpenAsync(CancellationToken cancellationToken) => Task.FromResult(false);

    public Task SetContextAsync(ImmutableArray<CodeDefinitionWindowLocation> locations, CancellationToken cancellationToken) => Task.CompletedTask;
}
