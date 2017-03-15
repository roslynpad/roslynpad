namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature
{
    internal sealed class SignatureChange
    {
        public SignatureChange(ParameterConfiguration originalConfiguration, ParameterConfiguration updatedConfiguration)
        {
            OriginalConfiguration = originalConfiguration;
            UpdatedConfiguration = updatedConfiguration;
        }

        public ParameterConfiguration OriginalConfiguration { get; }
        public ParameterConfiguration UpdatedConfiguration { get; }

        internal Microsoft.CodeAnalysis.ChangeSignature.SignatureChange ToInternal()
        {
            return new Microsoft.CodeAnalysis.ChangeSignature.SignatureChange(OriginalConfiguration.ToInternal(), UpdatedConfiguration.ToInternal());
        }
    }
}