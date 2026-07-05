using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    [DebuggerDisplay("{Participation}")]
    public struct CompletionStartData : IEquatable<CompletionStartData>
    {
        /// <summary>
        /// Value to use when <see cref="IAsyncCompletionSource"/> does not know the precise <see cref="SnapshotSpan"/> for completion,
        /// and does not want to participate in completion.
        /// </summary>
        public static CompletionStartData DoesNotParticipateInCompletion { get; } = new CompletionStartData(CompletionParticipation.DoesNotProvideItems, default);

        /// <summary>
        /// Value to use when <see cref="IAsyncCompletionSource"/> does not know the precise <see cref="SnapshotSpan"/> for completion,
        /// but wishes to participate in completion if language service can provide a valid <see cref="SnapshotSpan"/>.
        /// </summary>
        public static CompletionStartData ParticipatesInCompletionIfAny { get; } = new CompletionStartData(CompletionParticipation.ProvidesItems, default);

        /// <summary>
        /// Describes the level of <see cref="IAsyncCompletionSource"/>'s participation in the <see cref="IAsyncCompletionSession"/>.
        /// </summary>
        public CompletionParticipation Participation { get; }

        /// <summary>
        /// <param name="applicableToSpan"> Proposed location where completion will take place.
        /// Return <c>default</c> if this <see cref="IAsyncCompletionSource"/> is not capable of providing location,
        /// or completion is invalid for location in question.</param>
        /// </summary>
        public SnapshotSpan ApplicableToSpan { get; }

        public CompletionStartData(CompletionParticipation participation, SnapshotSpan applicableToSpan = default) : this()
        {
            Participation = participation;
            ApplicableToSpan = applicableToSpan;
        }

        bool IEquatable<CompletionStartData>.Equals(CompletionStartData other) => Participation.Equals(other.Participation) && ApplicableToSpan.Equals(other.ApplicableToSpan);

        public override bool Equals(object other) => (other is CompletionStartData otherCR) ? ((IEquatable<CompletionStartData>)this).Equals(otherCR) : false;

        public static bool operator ==(CompletionStartData left, CompletionStartData right) => left.Equals(right);

        public static bool operator !=(CompletionStartData left, CompletionStartData right) => !(left == right);

        public override int GetHashCode() => (ApplicableToSpan.GetHashCode() << 2) | ((int)Participation);
    }
}
