using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.Core.Interceptor;
using Microsoft.CodeAnalysis;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.LanguageServices.ExtractInterface
{
    internal sealed class ExtractInterfaceOptionsServiceProxy : IInterceptor
    {
        internal static readonly Type InterfaceType =
            Type.GetType("Microsoft.CodeAnalysis.ExtractInterface.IExtractInterfaceOptionsService, Microsoft.CodeAnalysis.Features");

        internal static readonly Lazy<Type> GeneratedType =
            new Lazy<Type>(() => RoslynInterfaceProxy.GenerateFor(InterfaceType, isWorkspaceService: true));

        void IInterceptor.Intercept(IInvocation invocation)
        {
            switch (invocation.Method.Name)
            {
                case nameof(GetExtractInterfaceOptions):
                    invocation.ReturnValue = GetExtractInterfaceOptions(
                        invocation.Arguments[0],
                        (List<ISymbol>)invocation.Arguments[2],
                        (string)invocation.Arguments[3],
                        (List<string>)invocation.Arguments[4],
                        (string)invocation.Arguments[5],
                        (string)invocation.Arguments[6],
                        (string)invocation.Arguments[7]);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private static object GetExtractInterfaceOptions(object syntaxFactsService, List<ISymbol> extractableMembers, string defaultInterfaceName, List<string> conflictingTypeNames, string defaultNamespace, string generatedNameTypeParameterSuffix, string languageName)
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
            return options.ToInternal();
        }
    }

    internal class ExtractInterfaceOptionsResult
    {
        private static readonly Type Type =
            Type.GetType("Microsoft.CodeAnalysis.ExtractInterface.ExtractInterfaceOptionsResult, Microsoft.CodeAnalysis.Features");

        public static readonly ExtractInterfaceOptionsResult Cancelled = new ExtractInterfaceOptionsResult(true);

        public bool IsCancelled { get; }

        public IEnumerable<ISymbol> IncludedMembers { get; }

        public string InterfaceName { get; }

        public string FileName { get; }

        public ExtractInterfaceOptionsResult(bool isCancelled, IEnumerable<ISymbol> includedMembers, string interfaceName, string fileName)
        {
            IsCancelled = isCancelled;
            IncludedMembers = includedMembers;
            InterfaceName = interfaceName;
            FileName = fileName;
        }

        private ExtractInterfaceOptionsResult(bool isCancelled)
        {
            IsCancelled = isCancelled;
        }

        internal object ToInternal()
        {
            if (this == Cancelled)
            {
                // ReSharper disable once PossibleNullReferenceException
                return Type.GetField(nameof(Cancelled), BindingFlags.Static | BindingFlags.Public).GetValue(null);
            }

            return Type.GetConstructors().First()
                    .Invoke(new object[] { IsCancelled, IncludedMembers, InterfaceName, FileName });
        }
    }
}