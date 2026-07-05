//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using Avalonia.Controls;

namespace Microsoft.VisualStudio.Text.Storage
{
    /// <summary>
    /// Provides a persistent data storage for items. Items are identified by textual keys and are retrieved as 
    /// <see cref="ResourceDictionary"/> objects.
    /// </summary>
    /// <remarks>
    /// TODO: For Dev11, this interface should provide methods to also write data (to disk or whatever underlying data storage) as opposed
    /// to only provide a method to retrieve values.
    /// </remarks>
    public interface IDataStorage
    {
        /// <summary>
        /// Retrieves the value of the item named itemKey.
        /// </summary>
        /// <returns><c>true</c> if item exists in the storage, <c>false</c> otherwise.</returns>
        bool TryGetItemValue(string itemKey, out ResourceDictionary itemValue);
    }
}
