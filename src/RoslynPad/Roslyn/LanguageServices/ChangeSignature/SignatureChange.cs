using System;

namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature
{
    internal sealed class SignatureChange
    {
        private static readonly Type Type = Type.GetType("Microsoft.CodeAnalysis.ChangeSignature.SignatureChange, Microsoft.CodeAnalysis.Features", throwOnError: true);

        public SignatureChange(ParameterConfiguration originalConfiguration, ParameterConfiguration updatedConfiguration)
        {
            OriginalConfiguration = originalConfiguration;
            UpdatedConfiguration = updatedConfiguration;
        }

        public ParameterConfiguration OriginalConfiguration { get; }
        public ParameterConfiguration UpdatedConfiguration { get; }

        internal object ToInternal()
        {
            var constructorInfo = Type.GetConstructor(new[] { ParameterConfiguration.Type, ParameterConfiguration.Type });
            if (constructorInfo == null)
            {
                throw new MissingMemberException("Internal constructor missing");
            }
            return constructorInfo
                .Invoke(new[] { OriginalConfiguration.ToInternal(), UpdatedConfiguration.ToInternal() });
        }
    }
}