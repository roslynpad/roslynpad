//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Represents a service provided by the CodeLens service infrastructure to allow CodeLens extensions to call back to VS.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part provided by the CodeLens service infrastructure, and can be imported by CodeLens extensions.
    /// Example:
    /// 
    /// <code>
    /// [Import]
    /// private ICodeLensCallbackService callbackService;
    /// </code>
    /// </remarks>
    [CLSCompliant(false)]
    public interface ICodeLensCallbackService
    {
        /// <summary>
        /// Gets a <see cref="JsonRpc"/> on which the <paramref name="dataPointProvider"/> can originate a callback to VS process.
        /// </summary>
        /// <param name="dataPointProvider">
        /// The <see cref="IAsyncCodeLensDataPointProvider"/> which the <see cref="JsonRpc"/> is associated with.
        /// </param>
        /// <returns>The <see cref="JsonRpc"/> that can be used to call back to VS process.</returns>
        /// <remarks>
        /// CodeLens extensions can use the <see cref="JsonRpc"/> returned from this method to invoke a callback to VS process
        /// using one of the JsonRpc.InvokeAsync overloads. The VS in-proc <see cref="ICodeLensCallbackListener"/>
        /// that has a method whose name or JsonRpcMethodAttribute exactly matches the target name passed to the
        /// callback invocation will receive the callback and can respond to the callback request with a result.
        ///
        /// Refer to JsonRpcMethodAttribute and JsonRpc.InvokeAsync for more detail.
        /// </remarks>
        /// <example>
        /// See <see cref="ICodeLensCallbackListener"/> for a callback example.
        /// </example>
        JsonRpc GetCallbackJsonRpc(IAsyncCodeLensDataPointProvider dataPointProvider);

        /// <summary>
        /// Gets a <see cref="JsonRpc"/> on which the <paramref name="dataPoint"/> can originate a callback to VS process.
        /// </summary>
        /// <param name="dataPoint">
        /// The <see cref="IAsyncCodeLensDataPoint"/> which the <see cref="JsonRpc"/> is associated with.
        /// </param>
        /// <returns>The <see cref="JsonRpc"/> that can be used to call back to VS process.</returns>
        /// <remarks>
        /// CodeLens extensions can use the <see cref="JsonRpc"/> returned from this method to invoke a callback to VS process
        /// using one of the JsonRpc.InvokeAsync overloads. The VS in-proc <see cref="ICodeLensCallbackListener"/>
        /// that has a method whose name or JsonRpcMethodAttribute exactly matches the target name passed to the
        /// callback invocation will receive the callback and can respond to the callback request with a result.
        ///
        /// Refer to JsonRpcMethodAttribute and JsonRpc.InvokeAsync for more detail.
        /// </remarks>
        /// <example>
        /// See <see cref="ICodeLensCallbackListener"/> for a callback example.
        /// </example>
        JsonRpc GetCallbackJsonRpc(IAsyncCodeLensDataPoint dataPoint);
    }
}
