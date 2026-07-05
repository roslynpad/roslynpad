//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Provides a custom implementation of span tracking. This delegate should be implemented by custom tracking spans.
    /// </summary>
    /// <param name="customSpan">The span to be tracked.</param>
    /// <param name="currentVersion">The version to which <paramref name="currentSpan"/> belongs.</param>
    /// <param name="targetVersion">The version to which <paramref name="currentSpan"/> is to be tracked.</param>
    /// <param name="currentSpan">The span to track.</param>
    /// <param name="customState">The custom state that was provided when the span was created.</param>
    /// <returns>The span to which <paramref name="currentSpan"/> tracks.</returns>
    /// <remarks><paramref name="targetVersion"/> may be earlier than <paramref name="currentVersion"/>.</remarks>
    public delegate Span CustomTrackToVersion(ITrackingSpan customSpan, ITextVersion currentVersion, ITextVersion targetVersion, Span currentSpan, object customState);
}