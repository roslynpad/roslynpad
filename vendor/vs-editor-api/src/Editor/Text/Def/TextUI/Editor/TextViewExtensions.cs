//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Utility <see cref="ITextView"/> extension methods.
    /// </summary>
    public static class TextViewExtensions
    {
        /// <summary>
        /// Gets whether given <see cref="ITextView"/> is embedded in another <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> for which to determine if it's embedded.</param>
        /// <returns><c>true</c> if given <see cref="ITextView"/> is embedded, <c>false</c> otherwise.</returns>
        public static bool IsEmbeddedTextView(this ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            return textView.Roles.Contains(PredefinedTextViewRoles.EmbeddedPeekTextView);
        }

        /// <summary>
        /// Gets containing <see cref="ITextView"/> for given embedded <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">An embedded <see cref="ITextView"/>, for which to get a containing <see cref="ITextView"/>.</param>
        /// <param name="containingTextView">A <see cref="ITextView"/> that contains given <see cref="ITextView"/> or null if
        /// given <see cref="ITextView"/> is not embedded in another <see cref="ITextView"/>.</param>
        /// <returns><c>true</c> if containing <see cref="ITextView"/> was found, <c>false</c> otherwise.</returns>
        public static bool TryGetContainingTextView(this ITextView textView, out ITextView containingTextView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            // Extra scrutiny because Peek is on a different layer and we cannot just rely on it doing the right thing
            if (textView.IsEmbeddedTextView())
            {
                bool success = textView.Properties.TryGetProperty("PeekContainingTextView", out containingTextView);
                if (!success || containingTextView == null)
                {
                    throw new InvalidOperationException("Unexpected failure to obtain containing text view of an embedded text view.");
                }

                return true;
            }

            containingTextView = null;
            return false;
        }

        /// <summary>
        /// Determines whether a view is in the process of being laid out or is preparing to be laid out.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> to check.</param>
        /// <remarks>
        /// As opposed to <see cref="ITextView.InLayout"/>, it is safe to get the <see cref="ITextView.TextViewLines"/>
        /// but attempting to queue another layout will cause a reentrant layout exception.
        /// </remarks>
        public static bool GetInOuterLayout(this ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            return ((ITextView2)textView).InOuterLayout;
        }

        /// <summary>
        /// Gets an object for managing selections within the view.
        /// </summary>
        public static IMultiSelectionBroker GetMultiSelectionBroker(this ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (textView is ITextView2 textView2)
            {
                return textView2.MultiSelectionBroker;
            }

            if (textView.Properties.TryGetProperty(typeof(IMultiSelectionBroker), out IMultiSelectionBroker broker))
            {
                return broker;
            }

            Debug.Fail("Failed to acquire IMultiSelectionBroker for a text view");

            return null;
        }

        /// <summary>
        /// See <see cref="ITextView2.QueuePostLayoutAction(Action)"/>.
        /// </summary>
        public static void QueuePostLayoutAction(this ITextView textView, Action action)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            (textView as ITextView2)?.QueuePostLayoutAction(action);
        }

        /// <summary>
        /// See <see cref="ITextView2.TryGetTextViewLines(out ITextViewLineCollection)"/>.
        /// </summary>
        public static bool TryGetTextViewLines(this ITextView textView, out ITextViewLineCollection textViewLines)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (textView is ITextView2 textView2)
            {
                return textView2.TryGetTextViewLines(out textViewLines);
            }
            else
            {
                textViewLines = null;
                return false;
            }
        }

        /// <summary>
        /// See <see cref="ITextView2.TryGetTextViewLineContainingBufferPosition(SnapshotPoint, out Formatting.ITextViewLine)"/>.
        /// </summary>
        public static bool TryGetTextViewLineContainingBufferPosition(this ITextView textView, SnapshotPoint bufferPosition, out ITextViewLineCollection textViewLines)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (textView is ITextView2 textView2)
            {
                return textView2.TryGetTextViewLineContainingBufferPosition(bufferPosition, out textViewLines);
            }
            else
            {
                textViewLines = null;
                return false;
            }
        }
    }
}
