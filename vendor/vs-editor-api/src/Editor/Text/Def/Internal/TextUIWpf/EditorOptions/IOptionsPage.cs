using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.OptionDescriptions
{
    public interface IOptionsPage
    {
        IEditorOptions Options { get; }
        IOptionControl GetControl(string name);
    }
}
