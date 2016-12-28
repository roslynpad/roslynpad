using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;

namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature
{
    [ExportWorkspaceService(typeof(IChangeSignatureOptionsService))]
    internal sealed class ChangeSignatureOptionsService : IChangeSignatureOptionsService
    {
        private readonly ExportFactory<IChangeSignatureDialog> _dialogFactory;

        [ImportingConstructor]
        public ChangeSignatureOptionsService(ExportFactory<IChangeSignatureDialog> dialogFactory)
        {
            _dialogFactory = dialogFactory;
        }
        public ChangeSignatureOptionsResult GetChangeSignatureOptions(ISymbol symbol, Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration parameters,
            INotificationService notificationService)
        {
            var viewModel = new ChangeSignatureDialogViewModel(new ParameterConfiguration(parameters), symbol);

            var dialog = _dialogFactory.CreateExport().Value;
            dialog.ViewModel = viewModel;
            var result = dialog.Show();

            return result == true
                ? new ChangeSignatureOptionsResult { IsCancelled = false, UpdatedSignature = new SignatureChange(new ParameterConfiguration(parameters), viewModel.GetParameterConfiguration()).ToInternal() }
                : new ChangeSignatureOptionsResult { IsCancelled = true };
        }
    }
}