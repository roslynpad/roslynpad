﻿using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature;

[ExportWorkspaceService(typeof(IChangeSignatureOptionsService))]
[method: ImportingConstructor]
internal sealed class ChangeSignatureOptionsService(ExportFactory<IChangeSignatureDialog> dialogFactory) : IChangeSignatureOptionsService
{
    private readonly ExportFactory<IChangeSignatureDialog> _dialogFactory = dialogFactory;

    public ChangeSignatureOptionsResult? GetChangeSignatureOptions(SemanticDocument document, int positionForTypeBinding, ISymbol symbol, Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration parameters)
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
