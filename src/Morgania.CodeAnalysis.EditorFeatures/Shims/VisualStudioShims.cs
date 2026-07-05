// Shims for Visual Studio types that are referenced by recompiled Roslyn EditorFeatures
// source but are not part of the Morgania editor platform. These either satisfy unused
// using directives (empty namespaces) or provide minimal internal stand-ins for VS shell
// data types that only flow through Roslyn-internal interfaces.

using System;
using System.Collections.Generic;

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
