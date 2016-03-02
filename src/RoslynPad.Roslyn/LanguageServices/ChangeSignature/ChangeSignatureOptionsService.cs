using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature
{
    [ExportWorkspaceService(typeof(IChangeSignatureOptionsService))]
    internal sealed class ChangeSignatureOptionsService : IChangeSignatureOptionsService
    {
        public ChangeSignatureOptionsResult GetChangeSignatureOptions(ISymbol symbol, Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration parameters,
            INotificationService notificationService)
        {
            var viewModel = new ChangeSignatureDialogViewModel(new ParameterConfiguration(parameters), symbol);

            var dialog = new ChangeSignatureDialog(viewModel);
            dialog.SetOwnerToActive();
            var result = dialog.ShowDialog();

            return result == true
                ? new ChangeSignatureOptionsResult { IsCancelled = false, UpdatedSignature = new SignatureChange(new ParameterConfiguration(parameters), viewModel.GetParameterConfiguration()).ToInternal() }
                : new ChangeSignatureOptionsResult { IsCancelled = true };
        }
    }
}