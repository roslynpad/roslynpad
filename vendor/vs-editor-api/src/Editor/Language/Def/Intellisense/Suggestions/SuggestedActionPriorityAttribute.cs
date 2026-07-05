// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// MEF metadata attribute declaring a priority class an <see cref="ISuggestedActionsSourceProvider"/>
    /// provides actions for. May be applied multiple times, ordered from highest to lowest priority
    /// (see <see cref="DefaultOrderings"/>).
    /// </summary>
    [CLSCompliant(false)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SuggestedActionPriorityAttribute : MultipleBaseMetadataAttribute
    {
        public SuggestedActionPriorityAttribute(string priority)
        {
            SuggestedActionPriority = priority ?? throw new ArgumentNullException(nameof(priority));
        }

        public string SuggestedActionPriority { get; }
    }
}
