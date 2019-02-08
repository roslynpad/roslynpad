using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;

namespace RoslynPad.Roslyn.LanguageServices.ExtractInterface
{
    [ExportWorkspaceService(typeof(IExtractInterfaceOptionsService))]
    internal sealed class ExtractInterfaceOptionsService : IExtractInterfaceOptionsService
    {
        private readonly ExportFactory<IExtractInterfaceDialog> _dialogFactory;

        [ImportingConstructor]
        public ExtractInterfaceOptionsService(ExportFactory<IExtractInterfaceDialog> dialogFactory)
        {
            _dialogFactory = dialogFactory;
        }

        public Task<ExtractInterfaceOptionsResult> GetExtractInterfaceOptionsAsync(
            ISyntaxFactsService syntaxFactsService,
            INotificationService notificationService,
            List<ISymbol> extractableMembers,
            string defaultInterfaceName,
            List<string> allTypeNames,
            string defaultNamespace,
            string generatedNameTypeParameterSuffix,
            string languageName)
        {
            var viewModel = new ExtractInterfaceDialogViewModel(
                syntaxFactsService,
                defaultInterfaceName,
                extractableMembers,
                allTypeNames,
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

        private static ExtractInterfaceOptionsResult.ExtractLocation GetLocation(InterfaceDestination destination)
        {
            switch (destination)
            {
                case InterfaceDestination.CurrentFile: return ExtractInterfaceOptionsResult.ExtractLocation.SameFile;
                case InterfaceDestination.NewFile: return ExtractInterfaceOptionsResult.ExtractLocation.NewFile;
                default: throw new InvalidOperationException();
            }
        }
    }
}