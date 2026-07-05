using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Morgania.CodeAnalysis.Editor.LanguageServices.ExtractInterface;

[ExportWorkspaceService(typeof(IExtractInterfaceOptionsService))]
[method: ImportingConstructor]
internal sealed class ExtractInterfaceOptionsService(ExportFactory<IExtractInterfaceDialog> dialogFactory) : IExtractInterfaceOptionsService
{
    private readonly ExportFactory<IExtractInterfaceDialog> _dialogFactory = dialogFactory;

    public ExtractInterfaceOptionsResult GetExtractInterfaceOptions(Document document, ImmutableArray<ISymbol> extractableMembers, string defaultInterfaceName, ImmutableArray<string> conflictingTypeNames, string defaultNamespace, string generatedNameTypeParameterSuffix)
    {
        var viewModel = new ExtractInterfaceDialogViewModel(
            document.GetRequiredLanguageService<ISyntaxFactsService>(),
            defaultInterfaceName,
            extractableMembers,
            conflictingTypeNames,
            defaultNamespace,
            generatedNameTypeParameterSuffix,
            document.Project.Language);

        var dialog = _dialogFactory.CreateExport().Value;
        dialog.ViewModel = viewModel;
        var options = dialog.Show() == true
            ? new ExtractInterfaceOptionsResult(
                isCancelled: false,
                includedMembers: viewModel.MemberContainers.Where(c => c.IsChecked).Select(c => c.MemberSymbol).AsImmutable(),
                interfaceName: viewModel.InterfaceName.Trim(),
                fileName: viewModel.FileName.Trim(),
                location: GetLocation(viewModel.Destination))
            : ExtractInterfaceOptionsResult.Cancelled;
        return options;
    }

    private static ExtractInterfaceOptionsResult.ExtractLocation GetLocation(InterfaceDestination destination) => destination switch
    {
        InterfaceDestination.CurrentFile => ExtractInterfaceOptionsResult.ExtractLocation.SameFile,
        InterfaceDestination.NewFile => ExtractInterfaceOptionsResult.ExtractLocation.NewFile,
        _ => throw new InvalidOperationException(),
    };
}
