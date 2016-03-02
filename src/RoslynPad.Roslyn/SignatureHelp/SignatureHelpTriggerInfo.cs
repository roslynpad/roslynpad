namespace RoslynPad.Roslyn.SignatureHelp
{
    public struct SignatureHelpTriggerInfo
    {
        internal Microsoft.CodeAnalysis.Editor.SignatureHelpTriggerInfo Inner { get; }

        public SignatureHelpTriggerReason TriggerReason => (SignatureHelpTriggerReason)Inner.TriggerReason;

        public char? TriggerCharacter => Inner.TriggerCharacter;

        public SignatureHelpTriggerInfo(SignatureHelpTriggerReason triggerReason, char? triggerCharacter = null)
        {
            Inner = new Microsoft.CodeAnalysis.Editor.SignatureHelpTriggerInfo(
                (Microsoft.CodeAnalysis.Editor.SignatureHelpTriggerReason)triggerReason, triggerCharacter);
        }
    }
}