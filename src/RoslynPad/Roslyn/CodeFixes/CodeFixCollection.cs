using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.CodeFixes
{
    public sealed class CodeFixCollection
    {
        public object Provider { get; }

        public TextSpan TextSpan { get; }

        public ImmutableArray<CodeFix> Fixes { get; }

        public FixAllCodeActionContext FixAllContext { get; }

        public CodeFixCollection(object inner)
        {
            Provider = inner.GetPropertyValue<object>(nameof(Provider));
            TextSpan = inner.GetPropertyValue<TextSpan>(nameof(TextSpan));
            Fixes = inner.GetPropertyValue<IEnumerable<object>>(nameof(Fixes))
                .Select(x => new CodeFix(x)).ToImmutableArray();
            var fixAllContext = inner.GetPropertyValue<FixAllContext>(nameof(FixAllContext));
            if (fixAllContext != null)
            {
                FixAllContext = fixAllContext.Document != null
                  ? new FixAllCodeActionContext(fixAllContext, fixAllContext.Document)
                  : new FixAllCodeActionContext(fixAllContext, fixAllContext.Project);
            }
        }
    }
}