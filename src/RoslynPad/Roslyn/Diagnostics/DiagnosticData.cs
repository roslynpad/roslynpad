using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.Diagnostics
{
    public sealed class DiagnosticData
    {
        public string Id { get; }
        public string Category { get; }

        public string Message { get; }
        public string Description { get; }
        public string Title { get; }
        public string HelpLink { get; }
        public DiagnosticSeverity Severity { get; }
        public DiagnosticSeverity DefaultSeverity { get; }
        public bool IsEnabledByDefault { get; }
        public int WarningLevel { get; }
        public IReadOnlyList<string> CustomTags { get; }
        public ImmutableDictionary<string, string> Properties { get; }
        public bool IsSuppressed { get; }

        public Workspace Workspace { get; }
        public ProjectId ProjectId { get; }

        public DiagnosticDataLocation DataLocation { get; }
        public IReadOnlyCollection<DiagnosticDataLocation> AdditionalLocations { get; }

        public DiagnosticData(object inner)
        {
            Id = inner.GetFieldValue<string>(nameof(Id));
            Category = inner.GetFieldValue<string>(nameof(Category));
            Message = inner.GetFieldValue<string>(nameof(Message));
            Description = inner.GetFieldValue<string>(nameof(Description));
            Title = inner.GetFieldValue<string>(nameof(Title));
            HelpLink = inner.GetFieldValue<string>(nameof(HelpLink));
            Severity = inner.GetFieldValue<DiagnosticSeverity>(nameof(Severity));
            DefaultSeverity = inner.GetFieldValue<DiagnosticSeverity>(nameof(DefaultSeverity));
            IsEnabledByDefault = inner.GetFieldValue<bool>(nameof(IsEnabledByDefault));
            WarningLevel = inner.GetFieldValue<int>(nameof(WarningLevel));
            CustomTags = inner.GetFieldValue<IReadOnlyList<string>>(nameof(CustomTags));
            Properties = inner.GetFieldValue<ImmutableDictionary<string, string>>(nameof(Properties));
            IsSuppressed = inner.GetFieldValue<bool>(nameof(IsSuppressed));
            Workspace = inner.GetFieldValue<Workspace>(nameof(Workspace));
            ProjectId = inner.GetFieldValue<ProjectId>(nameof(ProjectId));
            DataLocation = new DiagnosticDataLocation(inner.GetFieldValue<object>(nameof(DataLocation)));
            AdditionalLocations = inner.GetFieldValue<IEnumerable<object>>(nameof(AdditionalLocations))
                .Select(x => new DiagnosticDataLocation(x)).ToImmutableArray();
        }

        public DocumentId DocumentId => DataLocation?.DocumentId;

        public bool HasTextSpan => (DataLocation?.SourceSpan).HasValue;

        // ReSharper disable once PossibleInvalidOperationException
        public TextSpan TextSpan => (DataLocation?.SourceSpan).Value;
    }
}