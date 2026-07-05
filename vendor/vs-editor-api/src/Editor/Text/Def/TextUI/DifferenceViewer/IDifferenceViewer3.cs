//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    using System;

    public interface IDifferenceViewer3 : IDifferenceViewer2
    {
        /// <summary>
        /// Should the differences be displayed?
        /// </summary>
        /// <remarks>
        /// <para>This will be true if and only if there is a baseline and if the <see cref="DifferenceViewerOptions.ShowDifferencesId"/> option is true.</para>
        /// <para><see cref="IDifferenceViewer.ViewModeChanged"/> will be raised whenever this value changes.</para>
        /// </remarks>
        bool DisplayDifferences { get; }
    }
}
