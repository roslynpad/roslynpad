using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.QuickInfo
{
    public interface IQuickInfoProvider
    {
        Task<QuickInfoItem> GetItemAsync(Document document, int position, CancellationToken cancellationToken);
    }

    public sealed class QuickInfoItem
    {
        private readonly Func<object> _contentFactory;

        public TextSpan TextSpan { get; }

        public object Create() => _contentFactory();

        internal QuickInfoItem(TextSpan textSpan, Func<object> contentFactory)
        {
            TextSpan = textSpan;
            _contentFactory = contentFactory;
        }
    }
}