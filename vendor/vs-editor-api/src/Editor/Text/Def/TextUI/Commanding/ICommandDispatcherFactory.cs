namespace Microsoft.VisualStudio.Text.Editor.Commanding
{
    public interface ICommandDispatcherFactory
    {
        ISelectorCommandDispatcher GetDispatcher(ITextView textView);
    }
}