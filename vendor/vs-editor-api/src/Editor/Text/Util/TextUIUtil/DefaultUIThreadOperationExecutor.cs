using System;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.UI.Text.Commanding.Implementation
{
    [System.Composition.Shared]
    [ExportImplementation(typeof(IUIThreadOperationExecutor))]
    [Name("default")]
    public class DefaultUIThreadOperationExecutor : IUIThreadOperationExecutor
    {
        public IUIThreadOperationContext BeginExecute(string title, string defaultDescription, bool allowCancellation, bool showProgress)
        {
            return BeginExecute(new UIThreadOperationExecutionOptions(title, defaultDescription, allowCancellation, showProgress));
        }

        public IUIThreadOperationContext BeginExecute(UIThreadOperationExecutionOptions executionOptions)
        {
            return new DefaultUIThreadOperationContext(executionOptions.AllowCancellation, executionOptions.DefaultDescription);
        }

        public UIThreadOperationStatus Execute(string title, string defaultDescription, bool allowCancellation, bool showProgress, Action<IUIThreadOperationContext> action)
        {
            return Execute(new UIThreadOperationExecutionOptions(title, defaultDescription, allowCancellation, showProgress), action);
        }

        public UIThreadOperationStatus Execute(UIThreadOperationExecutionOptions executionOptions, Action<IUIThreadOperationContext> action)
        {
            var context = new DefaultUIThreadOperationContext(executionOptions.AllowCancellation, executionOptions.DefaultDescription);
            action(context);
            return UIThreadOperationStatus.Completed;
        }
    }

    internal class DefaultUIThreadOperationContext : AbstractUIThreadOperationContext
    {
        public DefaultUIThreadOperationContext(bool allowCancellation, string defaultDescription)
            : base(allowCancellation, defaultDescription)
        {
        }
    }
}
