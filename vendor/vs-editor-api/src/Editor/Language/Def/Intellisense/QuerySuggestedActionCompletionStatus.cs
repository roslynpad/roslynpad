// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
#pragma warning disable CA1717 // Only FlagsAttribute enums should have plural names
    /// <summary>
    /// Represents the completion status of querying LightBulb providers for suggested actions.
    /// </summary>
    public enum QuerySuggestedActionCompletionStatus
#pragma warning restore CA1717 // Only FlagsAttribute enums should have plural names
    {
        /// <summary>
        /// Querying LightBulb providers for suggested actions completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Querying LightBulb providers for suggested actions was cancelled.
        /// </summary>
        Canceled
    }
}
