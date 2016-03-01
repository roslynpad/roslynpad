using System;

namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature
{
    internal sealed class ChangeSignatureOptionsResult
    {
        private static readonly Type Type = Type.GetType("Microsoft.CodeAnalysis.ChangeSignature.ChangeSignatureOptionsResult, Microsoft.CodeAnalysis.Features", throwOnError: true);

        public bool IsCancelled { get; set; }
        public bool PreviewChanges { get; set; }
        public SignatureChange UpdatedSignature { get; set; }

        internal object ToInternal()
        {
            var instance = Activator.CreateInstance(Type);
            Type.GetProperty(nameof(IsCancelled)).SetValue(instance, IsCancelled);
            Type.GetProperty(nameof(PreviewChanges)).SetValue(instance, PreviewChanges);
            Type.GetProperty(nameof(UpdatedSignature)).SetValue(instance, UpdatedSignature.ToInternal());
            return instance;
        }
    }
}