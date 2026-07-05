using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// Describes the level of <see cref="IAsyncCompletionSource"/>'s participation in the <see cref="IAsyncCompletionSession"/>.
    /// </summary>
    public enum CompletionParticipation
    {
        /// <summary>
        /// This <see cref="IAsyncCompletionSource"/> will not provide completion items.
        /// <see cref="CompletionStartData.ApplicableToSpan"/> returned by this <see cref="IAsyncCompletionSource"/> may be used
        /// in the prospective <see cref="IAsyncCompletionSession"/> if another <see cref="IAsyncCompletionSource"/> announced
        /// participation in completion.
        /// </summary>
        DoesNotProvideItems = 0,

        /// <summary>
        /// <see cref="IAsyncCompletionSource.GetCompletionContextAsync(IAsyncCompletionSession, CompletionTrigger, Text.SnapshotPoint, Text.SnapshotSpan, System.Threading.CancellationToken)"/>
        /// will be invoked, unless another <see cref="IAsyncCompletionSource"/>s returned <see cref="CompletionParticipation.ExclusivelyProvidesItems"/>.
        /// </summary>
        ProvidesItems = 1,

        /// <summary>
        /// <see cref="IAsyncCompletionSource.GetCompletionContextAsync(IAsyncCompletionSession, CompletionTrigger, Text.SnapshotPoint, Text.SnapshotSpan, System.Threading.CancellationToken)"/>
        /// will be invoked only on this <see cref="IAsyncCompletionSource"/> and other <see cref="IAsyncCompletionSource"/>s which returned <see cref="CompletionParticipation.ExclusivelyProvidesItems"/>.
        /// </summary>
        ExclusivelyProvidesItems = 2,
    }
}
