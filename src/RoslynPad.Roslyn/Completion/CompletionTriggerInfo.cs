namespace RoslynPad.Roslyn.Completion
{
    public struct CompletionTriggerInfo
    {
        internal Microsoft.CodeAnalysis.Completion.CompletionTriggerInfo Inner { get; }

        public CompletionTriggerReason TriggerReason => (CompletionTriggerReason)Inner.TriggerReason;

        public char? TriggerCharacter => Inner.TriggerCharacter;

        private CompletionTriggerInfo(Microsoft.CodeAnalysis.Completion.CompletionTriggerInfo inner)
        {
            Inner = inner;
        }

        public static CompletionTriggerInfo CreateTypeCharTriggerInfo(char triggerCharacter)
        {
            return new CompletionTriggerInfo(
                Microsoft.CodeAnalysis.Completion.CompletionTriggerInfo.CreateTypeCharTriggerInfo(triggerCharacter));
        }

        public static CompletionTriggerInfo CreateInvokeCompletionTriggerInfo()
        {
            return new CompletionTriggerInfo(Microsoft.CodeAnalysis.Completion.CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo());
        }

        public static CompletionTriggerInfo CreateBackspaceTriggerInfo(char? triggerCharacter)
        {
            return new CompletionTriggerInfo(Microsoft.CodeAnalysis.Completion.CompletionTriggerInfo.CreateBackspaceTriggerInfo(triggerCharacter));
        }

        public static CompletionTriggerInfo CreateSnippetTriggerInfo()
        {
            return new CompletionTriggerInfo(Microsoft.CodeAnalysis.Completion.CompletionTriggerInfo.CreateSnippetTriggerInfo());
        }
    }
}