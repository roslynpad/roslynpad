// Copyright (c) Microsoft Corporation
// All rights reserved

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Extends <see cref="ISuggestedAction2"/>. Marker interface used by newer hosts to
    /// identify actions supporting the extended display contract.
    /// </summary>
    [CLSCompliant(false)]
    public interface ISuggestedAction3 : ISuggestedAction2
    {
    }
}
