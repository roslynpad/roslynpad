using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RoslynPad.Roslyn.SignatureHelp
{
    public struct SignatureHelpTriggerInfo
    {
        internal object Inner { get; set; }

        public SignatureHelpTriggerReason TriggerReason { get; }

        public char? TriggerCharacter { get; }

        private static readonly Func<int, char?, object> _signatureHelpTriggerInfoCtor =
            CreateSignatureHelpTriggerInfoFunc();

        private static Func<int, char?, object> CreateSignatureHelpTriggerInfoFunc()
        {
            var type = Type.GetType("Microsoft.CodeAnalysis.Editor.SignatureHelpTriggerInfo, Microsoft.CodeAnalysis.EditorFeatures", throwOnError: true);
            var param = new[]
            {
                Expression.Parameter(typeof (int)),
                Expression.Parameter(typeof (char?)),
            };
            var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First();
            return Expression.Lambda<Func<int, char?, object>>(
                Expression.Convert(Expression.New(constructor, Expression.Convert(param[0], constructor.GetParameters()[0].ParameterType), param[1]), typeof(object)),
                param).Compile();
        }

        public SignatureHelpTriggerInfo(SignatureHelpTriggerReason triggerReason, char? triggerCharacter = null)
        {
            TriggerReason = triggerReason;
            TriggerCharacter = triggerCharacter;
            Inner = _signatureHelpTriggerInfoCtor((int)triggerReason, triggerCharacter);
        }
    }
}