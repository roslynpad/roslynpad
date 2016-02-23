using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.Diagnostics
{
    public sealed class DiagnosticDataLocation
    {
        public DocumentId DocumentId { get; }

        public TextSpan? SourceSpan { get; }

        public string MappedFilePath { get; }
        public int MappedStartLine { get; }
        public int MappedStartColumn { get; }
        public int MappedEndLine { get; }
        public int MappedEndColumn { get; }
        public string OriginalFilePath { get; }
        public int OriginalStartLine { get; }
        public int OriginalStartColumn { get; }
        public int OriginalEndLine { get; }
        public int OriginalEndColumn { get; }

        internal DiagnosticDataLocation(object inner)
        {
            DocumentId = inner.GetFieldValue<DocumentId>(nameof(DocumentId));
            SourceSpan = inner.GetFieldValue<TextSpan?>(nameof(SourceSpan));
            MappedFilePath = inner.GetFieldValue<string>(nameof(MappedFilePath));
            MappedStartLine = inner.GetFieldValue<int>(nameof(MappedStartLine));
            MappedStartColumn = inner.GetFieldValue<int>(nameof(MappedStartColumn));
            MappedEndLine = inner.GetFieldValue<int>(nameof(MappedEndLine));
            MappedEndColumn = inner.GetFieldValue<int>(nameof(MappedEndColumn));
            OriginalFilePath = inner.GetFieldValue<string>(nameof(OriginalFilePath));
            OriginalStartLine = inner.GetFieldValue<int>(nameof(OriginalStartLine));
            OriginalStartColumn = inner.GetFieldValue<int>(nameof(OriginalStartColumn));
            OriginalEndLine = inner.GetFieldValue<int>(nameof(OriginalEndLine));
            OriginalEndColumn = inner.GetFieldValue<int>(nameof(OriginalEndColumn));
        }
    }
}