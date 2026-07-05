//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

namespace Microsoft.VisualStudio.Text
{
    public static class ITextSnapshotExtensions
    {
        public static NormalizedSnapshotSpanCollection ToNormalizedSnapshotSpanCollection(this SnapshotSpan snapshotSpan)
        {
            return new NormalizedSnapshotSpanCollection(snapshotSpan);
        }

        public static bool IsMultiline(this SnapshotSpan snapshotSpan)
        {
            return snapshotSpan.Snapshot.GetLineNumberFromPosition(snapshotSpan.Start.Position) !=
                snapshotSpan.Snapshot.GetLineNumberFromPosition(snapshotSpan.End.Position);
        }

        public static SnapshotSpan GetEntireSpan(this ITextSnapshot textSnapshot)
        {
            return new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
        }

        public static SnapshotPoint? TryGetSnapshotPoint(this ITextSnapshot snapshot, int lineNumber, int columnIndex)
        {
            if (TryGetSnapshotPoint(snapshot, lineNumber, columnIndex, out var snapshotPoint))
            {
                return snapshotPoint;
            }

            return null;
        }

        public static bool TryGetSnapshotPoint(this ITextSnapshot snapshot, int lineNumber, int columnIndex, out SnapshotPoint position)
        {
            position = new SnapshotPoint();

            if (lineNumber < 0 || lineNumber >= snapshot.LineCount)
            {
                return false;
            }

            var line = snapshot.GetLineFromLineNumber(lineNumber);
            if (columnIndex < 0 || columnIndex >= line.LengthIncludingLineBreak)
            {
                return false;
            }

            int result = line.Start.Position + columnIndex;
            position = new SnapshotPoint(snapshot, result);
            return true;
        }

        public static void GetLineAndColumn(this SnapshotPoint point, out int lineNumber, out int columnIndex)
        {
            point.Snapshot.GetLineAndColumn(point.Position, out lineNumber, out columnIndex);
        }

        public static (int line, int column) GetLineAndColumn(this SnapshotPoint point)
        {
            GetLineAndColumn(point, out int line, out int column);
            return (line, column);
        }

        public static (int line, int column) GetLineAndColumn1Based(this SnapshotPoint point)
        {
            var (line, column) = GetLineAndColumn(point);
            return (line + 1, column + 1);
        }

        public static void GetLineAndColumn(this ITextSnapshot snapshot, int position, out int lineNumber, out int columnIndex)
        {
            var line = snapshot.GetLineFromPosition(position);

            lineNumber = line.LineNumber;
            columnIndex = position - line.Start.Position;
        }
    }
}