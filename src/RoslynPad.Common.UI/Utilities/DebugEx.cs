using System.Diagnostics;
using System.Threading;

namespace RoslynPad.UI.Utilities
{
    internal static class DebugEx
    {
        [Conditional("DEBUG")]
        public static void AssertIsUiThread()
        {
            Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "Not on UI thread");
        }
    }
}
