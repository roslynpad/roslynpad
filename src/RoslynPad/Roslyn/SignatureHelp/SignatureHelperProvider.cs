using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.SignatureHelp
{
    internal sealed class SignatureHelperProvider : ISignatureHelpProvider
    {
        internal static readonly Type InterfaceType = Type.GetType("Microsoft.CodeAnalysis.Editor.ISignatureHelpProvider, Microsoft.CodeAnalysis.EditorFeatures", throwOnError: true);
        
        private readonly object _inner;

        internal SignatureHelperProvider(object inner)
        {
            _inner = inner;
        }

        private static readonly Func<object, char, bool> _isTriggerCharacter = CreateIsTriggerCharacter();

        private static Func<object, char, bool> CreateIsTriggerCharacter()
        {
            var param = new[]
            {
                Expression.Parameter(typeof (object)),
                Expression.Parameter(typeof (char)),
            };
            return Expression.Lambda<Func<object, char, bool>>(
                Expression.Call(Expression.Convert(param[0], InterfaceType), InterfaceType.GetMethod(nameof(IsTriggerCharacter)), param[1]),
                param).Compile();
        }

        private static readonly Func<object, char, bool> _isRetriggerCharacter = CreateIsRetriggerCharacter();

        private static Func<object, char, bool> CreateIsRetriggerCharacter()
        {
            var param = new[]
            {
                Expression.Parameter(typeof (object)),
                Expression.Parameter(typeof (char)),
            };
            return Expression.Lambda<Func<object, char, bool>>(
                Expression.Call(Expression.Convert(param[0], InterfaceType), InterfaceType.GetMethod(nameof(IsRetriggerCharacter)), param[1]),
                param).Compile();
        }

        private static readonly Func<object, Document, int, object, CancellationToken, Task<object>> _getItemsAsync = CreateGetItemsAsync();

        private static Func<object, Document, int, object, CancellationToken, Task<object>> CreateGetItemsAsync()
        {
            var param = new[]
            {
                Expression.Parameter(typeof (object)),
                Expression.Parameter(typeof (Document)),
                Expression.Parameter(typeof (int)),
                Expression.Parameter(typeof (object)),
                Expression.Parameter(typeof (CancellationToken)),
            };
            var methodInfo = InterfaceType.GetMethod(nameof(GetItemsAsync));
            return Expression.Lambda<Func<object, Document, int, object, CancellationToken, Task<object>>>(
                Expression.Call(typeof(Utilities.TaskExtensions).GetMethod(nameof(Utilities.TaskExtensions.Cast), BindingFlags.Static | BindingFlags.Public)
                    .MakeGenericMethod(methodInfo.ReturnType.GetGenericArguments()[0], typeof(object)),
                    Expression.Call(Expression.Convert(param[0], InterfaceType), methodInfo, param[1], param[2],
                        Expression.Convert(param[3], methodInfo.GetParameters()[2].ParameterType), param[4])),
                param).Compile();
        }

        public bool IsTriggerCharacter(char ch)
        {
            return _isTriggerCharacter(_inner, ch);
        }

        public bool IsRetriggerCharacter(char ch)
        {
            return _isRetriggerCharacter(_inner, ch);
        }

        public async Task<SignatureHelpItems> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo,
            CancellationToken cancellationToken)
        {
            var result = await _getItemsAsync(_inner, document, position, triggerInfo.Inner, cancellationToken).ConfigureAwait(false);

            return result == null ? null : new SignatureHelpItems(result);
        }
    }
}