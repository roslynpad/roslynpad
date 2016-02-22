using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RoslynPad.Roslyn.Completion
{
    public struct CompletionTriggerInfo
    {
        internal object Inner { get; set; }

        public CompletionTriggerReason TriggerReason { get; }

        public char? TriggerCharacter { get; }

        private static readonly Func<int, char?, object> _completionTriggerInfoCtor =
            CreateCompletionTriggerInfoFunc();

        private static Func<int, char?, object> CreateCompletionTriggerInfoFunc()
        {
            var type = Type.GetType("Microsoft.CodeAnalysis.Completion.CompletionTriggerInfo, Microsoft.CodeAnalysis.Features", throwOnError: true);
            var param = new[]
            {
                Expression.Parameter(typeof (int)),
                Expression.Parameter(typeof (char?)),
            };
            var constructor = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
            return Expression.Lambda<Func<int, char?, object>>(
                Expression.Convert(Expression.New(constructor, Expression.Convert(param[0], constructor.GetParameters()[0].ParameterType), param[1]), typeof(object)),
                param).Compile();
        }

        private CompletionTriggerInfo(CompletionTriggerReason triggerReason, char? triggerCharacter)
        {
            TriggerReason = triggerReason;
            TriggerCharacter = triggerCharacter;
            Inner = _completionTriggerInfoCtor((int)triggerReason, triggerCharacter);
        }

        public static CompletionTriggerInfo CreateTypeCharTriggerInfo(char triggerCharacter)
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.TypeCharCommand, triggerCharacter);
        }

        public static CompletionTriggerInfo CreateInvokeCompletionTriggerInfo()
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.InvokeCompletionCommand, null);
        }

        public static CompletionTriggerInfo CreateBackspaceTriggerInfo(char? triggerCharacter)
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.BackspaceOrDeleteCommand, triggerCharacter);
        }

        public static CompletionTriggerInfo CreateSnippetTriggerInfo()
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.Snippets, null);
        }
    }
}