//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Projection
{
    public interface IProjectionSnapshot2 : IProjectionSnapshot
    {
        /// <summary>
        /// Computes the snapshot of <paramref name="targetBuffer"/> that is a contributor to this snapshot. If
        /// <paramref name="targetBuffer"/> is not in the source closure of this snapshot, return null.
        /// </summary>
        /// <exception cref="ArgumentNullException"> if <paramref name="targetBuffer"/> is null.</exception>
        ITextSnapshot GetMatchingSnapshotInClosure(ITextBuffer targetBuffer);

        /// <summary>
        /// For each snapshot in the source closure of this snapshot, call the <paramref name="match"/> predicate on the
        /// corresponding text buffer, and return the first source snapshot for which it returns true. The order in which the 
        /// source snapshots are visited is undefined.
        /// </summary>
        /// <exception cref="ArgumentNullException"> if <paramref name="match"/> is null.</exception>
        ITextSnapshot GetMatchingSnapshotInClosure(Predicate<ITextBuffer> match);
    }
}
