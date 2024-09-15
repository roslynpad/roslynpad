using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;

namespace RoslynPad.Roslyn.LanguageServices.ExtractInterface;

[ExportWorkspaceService(typeof(IExtractInterfaceOptionsService))]
[method: ImportingConstructor]
internal sealed class ExtractInterfaceOptionsService(ExportFactory<IExtractInterfaceDialog> dialogFactory) : IExtractInterfaceOptionsService
{
    private readonly ExportFactory<IExtractInterfaceDialog> _dialogFactory = dialogFactory;

    public Task<ExtractInterfaceOptionsResult> GetExtractInterfaceOptionsAsync(
        ISyntaxFactsService syntaxFactsService,
        INotificationService notificationService,
        List<ISymbol> extractableMembers,
        string defaultInterfaceName,
        List<string> conflictingTypeNames,
        string defaultNamespace,
        string generatedNameTypeParameterSuffix,
        string languageName,
        CancellationToken cancellationToken)
    {
        var viewModel = new ExtractInterfaceDialogViewModel(
            syntaxFactsService,
            defaultInterfaceName,
            extractableMembers,
            conflictingTypeNames,
            defaultNamespace,
            generatedNameTypeParameterSuffix,
            languageName,
            languageName == LanguageNames.CSharp ? ".cs" : ".vb");

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
        return Task.FromResult(options);
    }

    private static ExtractInterfaceOptionsResult.ExtractLocation GetLocation(InterfaceDestination destination) => destination switch
    {
        InterfaceDestination.CurrentFile => ExtractInterfaceOptionsResult.ExtractLocation.SameFile,
        InterfaceDestination.NewFile => ExtractInterfaceOptionsResult.ExtractLocation.NewFile,
        _ => throw new InvalidOperationException(),
    };
}
