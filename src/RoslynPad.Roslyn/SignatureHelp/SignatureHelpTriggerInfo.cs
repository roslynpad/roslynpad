namespace RoslynPad.Roslyn.SignatureHelp
{
    public struct SignatureHelpTriggerInfo
    {
        internal Microsoft.CodeAnalysis.SignatureHelp.SignatureHelpTriggerInfo Inner { get; }

        public SignatureHelpTriggerReason TriggerReason => (SignatureHelpTriggerReason)Inner.TriggerReason;

        public char? TriggerCharacter => Inner.TriggerCharacter;

        public SignatureHelpTriggerInfo(SignatureHelpTriggerReason triggerReason, char? triggerCharacter = null)
        {
            Inner = new Microsoft.CodeAnalysis.SignatureHelp.SignatureHelpTriggerInfo(
                (Microsoft.CodeAnalysis.SignatureHelp.SignatureHelpTriggerReason)triggerReason, triggerCharacter);
        }
    }
}