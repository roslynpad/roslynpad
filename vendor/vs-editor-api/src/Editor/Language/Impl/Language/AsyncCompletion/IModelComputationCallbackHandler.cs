using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    internal interface IModelComputationCallbackHandler<TModel>
    {
        Task UpdateUI(TModel model, CancellationToken token);
        void DismissDueToCancellation();
        void DismissDueToError();
        void ComputationFinished(TModel transformedModel);
    }
}
