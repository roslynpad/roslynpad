//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Defines the names of the protocols for CodeLens ServiceHub service.
    /// </summary>
    internal sealed class CodeLensServiceProtocolNames
    {
        /// <summary>
        /// The name of the default SeviceHub service.
        /// </summary>
        public const string CodeLensServiceName = "CodeLens";

        /// <summary>
        /// Protocol for getting CodeLens providers hosted by the service.
        /// </summary>
        /// <remarks>
        /// Protocol params and return:
        /// params: none
        /// return: <see cref="CodeLensDataPointProviderDescriptor"/>
        /// </remarks>
        public const string GetCodeLensProviders = @"codeLens/getProviders";

        /// <summary>
        /// Protocol for querying a CodeLens provider if it supports to create a data point.
        /// </summary>
        /// Protocol params and return:
        /// params: <see cref="CodeLensDescriptor"/> and the provider name.
        /// return: a boolean indicating whether the provider supports to create a data point. 
        /// <remarks>
        /// </remarks>
        public const string CanCreateDataPoint = @"codeLens/canCreateDataPoint";

        /// <summary>
        /// Protocol for retrieving data from a CodeLens data point.
        /// </summary>
        /// <remarks>
        /// Protocol params and return:
        /// params: <see cref="CodeLensDescriptor"/> and the provider name.
        /// return: <see cref="CodeLensDataPointDescriptor"/>
        /// </remarks>
        public const string GetCodeLensData = @"codeLens/getData";

        /// <summary>
        /// Protocol for retrieving details of a CodeLens data point.
        /// </summary>
        /// <remarks>
        /// Protocol params and return:
        /// params: <see cref="CodeLensDescriptor"/> and the provider name.
        /// return: <see cref="CodeLensDetailsDescriptor"/>
        /// </remarks>
        public const string GetCodeLensDetail = @"codeLens/getDetail";

        /// <summary>
        /// A protocol that the service can use to notify the CodeLens infrastructure
        /// that the remote CodeLens data in the data point source has been invalidated.
        /// </summary>
        /// <remarks>
        /// Protocol params and return:
        /// params: <see cref="CodeLensDescriptor"/> and the provider name
        /// return: none.
        /// </remarks>
        public const string NotifyInvalidation = @"codeLens/invalidate";
    }
}
