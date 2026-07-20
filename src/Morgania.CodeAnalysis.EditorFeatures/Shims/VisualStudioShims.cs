// Shims for Visual Studio types that are referenced by recompiled Roslyn EditorFeatures
// source but are not part of the Morgania editor platform. These either satisfy unused
// using directives (empty namespaces) or provide minimal internal stand-ins for VS shell
// data types that only flow through Roslyn-internal interfaces.

// Referenced by unused using directives in upstream Roslyn source.
namespace EnvDTE
{
    file sealed class Dummy;
}

namespace System.Runtime.Remoting.Contexts
{
    file sealed class Dummy;
}

namespace Microsoft.VisualStudio.TextManager.Interop
{
    file sealed class Dummy;
}

namespace Microsoft.VisualStudio.OLE.Interop
{
    file sealed class Dummy;
}

namespace Microsoft.VisualStudio.Shell.Interop
{
    // Used by NavigateTo preview services; previews are disabled outside VS.
    internal enum __VSPROVISIONALVIEWINGSTATUS
    {
        PVS_Disabled = 0,
        PVS_Enabled = 1,
    }
}

namespace System.Drawing
{
    // NavigateToItemDisplay.Glyph (legacy WinForms icon API) always returns null.
    internal sealed class Icon
    {
        private Icon()
        {
        }
    }
}

// API-shape drift between the VS editor Roslyn compiles against and the vendored one.
internal static class VisualStudioCompatExtensions
{
    extension(Microsoft.VisualStudio.Text.Editor.DefaultWpfViewOptions)
    {
        /// <summary>
        /// Upstream WPF option id; the vendored editor hosts this option on
        /// <see cref="Microsoft.VisualStudio.Text.Editor.DefaultTextViewOptions"/>.
        /// </summary>
        public static Microsoft.VisualStudio.Text.Editor.EditorOptionKey<bool> EnableHighlightCurrentLineId
            => Microsoft.VisualStudio.Text.Editor.DefaultTextViewOptions.EnableHighlightCurrentLineId;
    }
}
