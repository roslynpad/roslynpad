using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Diagnostics
{
    public sealed class DiagnosticData
    {
        private readonly Microsoft.CodeAnalysis.Diagnostics.DiagnosticData _inner;

        public string Id => _inner.Id;
        public string Category => _inner.Category;
        public string Message => _inner.Message;
        public string Description => _inner.Description;
        public string Title => _inner.Title;
        public string HelpLink => _inner.HelpLink;
        public DiagnosticSeverity Severity => _inner.Severity;
        public DiagnosticSeverity DefaultSeverity => _inner.DefaultSeverity;
        public bool IsEnabledByDefault => _inner.IsEnabledByDefault;
        public int WarningLevel => _inner.WarningLevel;
        public IReadOnlyList<string> CustomTags => _inner.CustomTags;
        public ImmutableDictionary<string, string> Properties => _inner.Properties;
        public bool IsSuppressed => _inner.IsSuppressed;
        public Workspace Workspace => _inner.Workspace;
        public ProjectId ProjectId => _inner.ProjectId;
        public DocumentId DocumentId => _inner.DocumentId;
        public bool HasTextSpan => _inner.HasTextSpan;
        public TextSpan TextSpan => _inner.TextSpan;

        public DiagnosticDataLocation DataLocation { get; }
        public IReadOnlyCollection<DiagnosticDataLocation> AdditionalLocations { get; }

        internal DiagnosticData(Microsoft.CodeAnalysis.Diagnostics.DiagnosticData inner)
        {
            _inner = inner;
            DataLocation = new DiagnosticDataLocation(inner.DataLocation);
            AdditionalLocations = inner.AdditionalLocations
                .Select(x => new DiagnosticDataLocation(x)).ToImmutableArray();
        }
    }
}