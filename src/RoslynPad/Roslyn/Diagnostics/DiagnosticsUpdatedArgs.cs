using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.Diagnostics
{
    public class DiagnosticsUpdatedArgs : UpdatedEventArgs
    {
        public DiagnosticsUpdatedKind Kind { get; }
        public Solution Solution { get; }
        public ImmutableArray<DiagnosticData> Diagnostics { get; }

        internal DiagnosticsUpdatedArgs(object inner) : base(inner)
        {
            Solution = inner.GetPropertyValue<Solution>(nameof(Solution));
            Diagnostics = inner.GetPropertyValue<IEnumerable<object>>(nameof(Diagnostics))
                .Select(x => new DiagnosticData(x)).ToImmutableArray();
            Kind = (DiagnosticsUpdatedKind)inner.GetPropertyValue<int>(nameof(Kind));
        }
    }
}