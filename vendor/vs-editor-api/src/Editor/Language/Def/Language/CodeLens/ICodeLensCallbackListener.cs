//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Language.CodeLens
{
    /// <summary>
    /// Represents an object in VS process that listens and responds to callback from CodeLens OOP extensions.
    /// The remote CodeLens extension calls back to VS through  <see cref="StreamJsonRpc.JsonRpc"/>
    /// which can be obtained from <see cref="Remoting.ICodeLensCallbackService"/>.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part loaded in to VS process and exported by CodeLens data point provider extenders.
    /// </remarks>
    /// <example>
    /// The implementer of this interface should make sure that the implementation has the callback method
    /// that exactly matches the target name passed to the <see cref="StreamJsonRpc.JsonRpc.InvokeAsync"/>
    /// in order to receive the callback originated by the InvokeAsync call.
    ///
    /// For example, the remote CodeLens extension originates a callback with JSON-RPC method "MyCallbackListener.Callback":
    ///
    /// <code>
    /// [Import]
    /// ICodeLensCallbackService callbackService;
    /// ...
    /// // Get the JsonRpc for the data point
    /// var jsonRpc = callbackService.GetCallbackJsonRpc(dataPoint);
    /// // Invoke the callback
    /// var result = await jsonRpc.InvokeAsync<MyDataType>("MyCallbackListener.Callback", argument);
    /// </code>
    ///
    /// The <see cref="ICodeLensCallbackListener"/> implementation that has the following <see cref="StreamJsonRpc.JsonRpcMethodAttribute"/>
    /// will receive the callback:
    ///
    /// <code>
    /// [Export(typeof(ICodeLensCallbackListener))]
    /// public sealed class CodeLensCallbackListener : ICodeLensCallbackListener
    /// {
    ///     [JsonRpcMethod("MyCallbackListener.Callback")]
    ///     public async Task<MyDataType> OnCallbackAsync(object argument)
    ///     {
    ///         ...
    ///         MyDataType result = await CalculateResult(argument);
    ///         return result;
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface ICodeLensCallbackListener
    {
        // intentionally no method defined for this interface.
    }
}
