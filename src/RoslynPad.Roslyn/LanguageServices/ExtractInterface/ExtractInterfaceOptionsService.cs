using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.LanguageServices.ExtractInterface
{
    [ExportWorkspaceService(typeof(IExtractInterfaceOptionsService))]
    internal sealed class ExtractInterfaceOptionsService : IExtractInterfaceOptionsService
    {
        public ExtractInterfaceOptionsResult GetExtractInterfaceOptions(ISyntaxFactsService syntaxFactsService,
            INotificationService notificationService, List<ISymbol> extractableMembers, string defaultInterfaceName,
            List<string> conflictingTypeNames, string defaultNamespace, string generatedNameTypeParameterSuffix, string languageName)
        {
            var viewModel = new ExtractInterfaceDialogViewModel(syntaxFactsService, defaultInterfaceName, extractableMembers, conflictingTypeNames, defaultNamespace, generatedNameTypeParameterSuffix, languageName, languageName == LanguageNames.CSharp ? ".cs" : ".vb");
            var dialog = new ExtractInterfaceDialog(viewModel);
            dialog.SetOwnerToActive();
            var options = dialog.ShowDialog() == true
                ? new ExtractInterfaceOptionsResult(
                    isCancelled: false,
                    includedMembers: viewModel.MemberContainers.Where(c => c.IsChecked).Select(c => c.MemberSymbol),
                    interfaceName: viewModel.InterfaceName.Trim(),
                    fileName: viewModel.FileName.Trim())
                : ExtractInterfaceOptionsResult.Cancelled;
            return options;
        }
    }
}