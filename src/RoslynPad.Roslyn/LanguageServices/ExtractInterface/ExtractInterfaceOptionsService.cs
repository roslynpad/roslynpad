using System.Collections.Generic;
using System.Composition;
using System.Linq;
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
        private readonly CompositionContext _context;

        [ImportingConstructor]
        public ExtractInterfaceOptionsService(CompositionContext context)
        {
            _context = context;
        }

        public ExtractInterfaceOptionsResult GetExtractInterfaceOptions(ISyntaxFactsService syntaxFactsService,
            INotificationService notificationService, List<ISymbol> extractableMembers, string defaultInterfaceName,
            List<string> conflictingTypeNames, string defaultNamespace, string generatedNameTypeParameterSuffix, string languageName)
        {
            var viewModel = new ExtractInterfaceDialogViewModel(syntaxFactsService, defaultInterfaceName, extractableMembers, conflictingTypeNames, defaultNamespace, generatedNameTypeParameterSuffix, languageName, languageName == LanguageNames.CSharp ? ".cs" : ".vb");
            var dialog = _context.GetExport<IExtractInterfaceDialog>();
            dialog.ViewModel = viewModel;
            var options = dialog.Show() == true
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