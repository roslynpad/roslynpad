using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Host.Mef;

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
        public ChangeSignatureOptionsResult? GetChangeSignatureOptions(Document document, int positionForTypeBinding, ISymbol symbol, Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration parameters)
        {
            var viewModel = new ChangeSignatureDialogViewModel(new ParameterConfiguration(parameters), symbol);

            var dialog = _dialogFactory.CreateExport().Value;
            dialog.ViewModel = viewModel;
            var result = dialog.Show();

            return result == true
                ? new ChangeSignatureOptionsResult(new SignatureChange(new ParameterConfiguration(parameters), viewModel.GetParameterConfiguration()).ToInternal(), previewChanges: false)
                : null;
        }
    }
}
