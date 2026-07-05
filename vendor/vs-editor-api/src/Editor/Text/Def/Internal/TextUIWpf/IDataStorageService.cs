//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Storage
{
    /// <summary>
    /// Implementers of this interface provide <see cref="IDataStorage"/> objects identified by a set of textual keys.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part and providers should export their implementations with the export attribute.
    /// </remarks>
    public interface IDataStorageService
    {
        /// <summary>
        /// Return a <see cref="IDataStorage"/> for the provided key.
        /// </summary>
        /// <returns>Null if no data storage for the provided key exists, otherwise returns the corresponding <see cref="IDataStorage"/>.</returns>
        IDataStorage GetDataStorage(string storageKey);
    }
}
