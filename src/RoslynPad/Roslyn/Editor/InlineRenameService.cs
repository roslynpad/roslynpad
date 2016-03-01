using System;
using System.Composition;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Editor
{
    [Export(typeof(IInlineRenameService)), Shared]
    internal sealed class InlineRenameService : IInlineRenameService
    {
        private static readonly Type InterfaceType = Type.GetType("Microsoft.CodeAnalysis.Editor.IInlineRenameService, Microsoft.CodeAnalysis.EditorFeatures", throwOnError: true);

        private readonly object _inner;

        [ImportingConstructor]
        public InlineRenameService(CompositionContext compositionContext)
        {
            _inner = compositionContext.GetExport(InterfaceType);
        }

        private static readonly Func<object, object> _actionSession = CreateActiveSession();

        private static Func<object, object> CreateActiveSession()
        {
            var p = Expression.Parameter(typeof(object));

            return Expression.Lambda<Func<object, object>>(
                Expression.Property(Expression.Convert(p, InterfaceType), InterfaceType.GetProperty(nameof(ActiveSession)))
                , p).Compile();
        }

        public IInlineRenameSession ActiveSession => new InlineRenameSession(_actionSession(_inner));

        private static readonly Func<object, Document, TextSpan, CancellationToken, object> _startInlineSession =
            CreateStartInlineSession();

        private static Func<object, Document, TextSpan, CancellationToken, object> CreateStartInlineSession()
        {
            var p = new[]
            {
                Expression.Parameter(typeof(object)),
                Expression.Parameter(typeof(Document)),
                Expression.Parameter(typeof(TextSpan)),
                Expression.Parameter(typeof(CancellationToken)),
            };
            return Expression.Lambda<Func<object, Document, TextSpan, CancellationToken, object>>(
                Expression.Call(Expression.Convert(p[0], InterfaceType), InterfaceType.GetMethod(nameof(StartInlineSession)),
                    p[1], p[2], p[3])
                , p).Compile();
        }

        public InlineRenameSessionInfo StartInlineSession(Document document, TextSpan triggerSpan,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return new InlineRenameSessionInfo(_startInlineSession(_inner, document, triggerSpan, cancellationToken));
        }
    }
}