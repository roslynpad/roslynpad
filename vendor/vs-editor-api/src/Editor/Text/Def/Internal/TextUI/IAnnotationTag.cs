//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using Microsoft.VisualStudio.Text.Tagging;

    public interface IAnnotationTag : ITag
    {
        /// <summary>
        /// Can the user navigate to the location of this item (errors, find matches, collapsed regions).
        /// </summary>
        bool IsNavigable { get; }

        /// <summary>
        ///  Some unique object where things of the same type (e.g. tracepoints) return the same object. Used to group similar things together.
        /// </summary>
        AnnotationKind ItemKindIdentifier { get; }

        /// <summary>
        /// What should be read out to indicate the existance of <paramref name="count"/> things of the same kind (e.g. "1 warning", or "2 errors").
        /// </summary>
        string ItemKindDisplayText(int count);
    }

    public abstract class AnnotationTag : IAnnotationTag
    {
        public virtual bool IsNavigable => false;

        public abstract AnnotationKind ItemKindIdentifier { get; }

        public abstract string ItemKindDisplayText(int count);
    }

    public enum AnnotationKind
    {
        Error = 1000,
        Warning = 2000,
        Message = 3000,
        InstructionPointer = 3500,
        Breakpoint = 4000,
        Shortcut = 5000,
        Tracepoint = 6000,
        Bookmark = 7000,
        CollapsedRegion = 8000,
        ExpandedRegion = 9000,
        Suggestion = 10000,
        LineAddition = 11000,
        LineDeletion = 12000,
        WordChange = 13000
    }
}
