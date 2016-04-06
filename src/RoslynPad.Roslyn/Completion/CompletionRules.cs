namespace RoslynPad.Roslyn.Completion
{
    public class CompletionRules
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly Microsoft.CodeAnalysis.Completion.CompletionRules _inner;

        internal CompletionRules(Microsoft.CodeAnalysis.Completion.CompletionRules inner)
        {
            _inner = inner;
        }
    }
}