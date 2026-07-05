#nullable enable

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation;

using System.Composition;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// The space reservation managers the IntelliSense presenters place popups through.
/// Definition order is reservation priority: completion reserves first (it anchors to the
/// caret line and must not move), signature help positions around it, and Quick Info around
/// both.
/// </summary>
public sealed class IntellisenseSpaceReservationManagers
{
    [Export]
    [Name(IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName)]
    [Order]
    public SpaceReservationManagerDefinition? CompletionManager { get; }

    [Export]
    [Name(IntellisenseSpaceReservationManagerNames.SignatureHelpSpaceReservationManagerName)]
    [Order(After = IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName)]
    public SpaceReservationManagerDefinition? SignatureHelpManager { get; }

    [Export]
    [Name(IntellisenseSpaceReservationManagerNames.QuickInfoSpaceReservationManagerName)]
    [Order(After = IntellisenseSpaceReservationManagerNames.SignatureHelpSpaceReservationManagerName)]
    public SpaceReservationManagerDefinition? QuickInfoManager { get; }
}
