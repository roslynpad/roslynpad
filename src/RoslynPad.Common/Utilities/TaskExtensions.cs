using System.Threading.Tasks;

namespace RoslynPad.Utilities
{
    internal static class TaskExtensions
    {
        public static async Task<TTarget> Cast<TSource, TTarget>(this Task<TSource> task)
        {
            var result = await task.ConfigureAwait(false);
            return (TTarget)(object)result;
        }
    }
}