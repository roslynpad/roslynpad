using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;

namespace RoslynPad.Roslyn.Completion
{
    [Export(typeof(IWaitIndicator))]
    internal sealed class DummyWaitIndicator : IWaitIndicator
    {
        public WaitIndicatorResult Wait(string title, string message, bool allowCancel, bool showProgress, Action<IWaitContext> action)
        {
            return WaitIndicatorResult.Completed;
        }

        public IWaitContext StartWait(string title, string message, bool allowCancel, bool showProgress)
        {
            return null;
        }
    }
}