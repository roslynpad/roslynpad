namespace Morgania.CodeAnalysis.Editor.LanguageServices.ChangeSignature;

internal sealed class SignatureChange(ParameterConfiguration originalConfiguration, ParameterConfiguration updatedConfiguration)
{
    public ParameterConfiguration OriginalConfiguration { get; } = originalConfiguration;
    public ParameterConfiguration UpdatedConfiguration { get; } = updatedConfiguration;

    internal Microsoft.CodeAnalysis.ChangeSignature.SignatureChange ToInternal()
    {
        return new Microsoft.CodeAnalysis.ChangeSignature.SignatureChange(OriginalConfiguration.ToInternal(), UpdatedConfiguration.ToInternal());
    }
}