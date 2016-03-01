using System;
using Castle.Core.Interceptor;
using Microsoft.CodeAnalysis;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature
{
    internal sealed class ChangeSignatureOptionsServiceProxy : IInterceptor
    {
        internal static readonly Type InterfaceType = Type.GetType("Microsoft.CodeAnalysis.ChangeSignature.IChangeSignatureOptionsService, Microsoft.CodeAnalysis.Features", throwOnError: true);

        internal static readonly Lazy<Type> GeneratedType =
            new Lazy<Type>(() => RoslynInterfaceProxy.GenerateFor(InterfaceType, isWorkspaceService: true));

        void IInterceptor.Intercept(IInvocation invocation)
        {
            switch (invocation.Method.Name)
            {
                case nameof(GetChangeSignatureOptions):
                    invocation.ReturnValue = GetChangeSignatureOptions(
                        (ISymbol) invocation.Arguments[0],
                        new ParameterConfiguration(invocation.Arguments[1])).ToInternal();
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public ChangeSignatureOptionsResult GetChangeSignatureOptions(ISymbol symbol, ParameterConfiguration parameters)
        {
            var viewModel = new ChangeSignatureDialogViewModel(parameters, symbol);

            var dialog = new ChangeSignatureDialog(viewModel);
            dialog.SetOwnerToActive();
            var result = dialog.ShowDialog();

            return result == true 
                ? new ChangeSignatureOptionsResult { IsCancelled = false, UpdatedSignature = new SignatureChange(parameters, viewModel.GetParameterConfiguration()) }
                : new ChangeSignatureOptionsResult { IsCancelled = true };
        }
    }
}