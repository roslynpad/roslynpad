using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// A status bar service enabling to send messages to the editor host's status bar.
    /// </summary>
    /// <remarks>
    /// <para>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IStatusBarService statusBarService = null;
    /// </para>
    /// </remarks>
    public interface IStatusBarService
    {
        /// <summary>
        /// Sends a text to the editor host's status bar.
        /// </summary>
        /// <param name="text">A text to be displayed on the status bar.</param>
        Task SetTextAsync(string text);
    }
}
