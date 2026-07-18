using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Navigation;

/// <summary>
/// The app side of Roslyn navigation: opening/activating document tabs is UI policy, so the
/// workspace services delegate here. Implemented by <c>MainViewModel</c>, which registers
/// itself on the <see cref="NavigationBridge"/> once the Roslyn host is up.
/// </summary>
public interface INavigationHost
{
    /// <summary>Activates the tab showing <paramref name="documentId"/> and selects <paramref name="span"/>.</summary>
    Task<bool> NavigateToDocumentAsync(DocumentId documentId, TextSpan span, CancellationToken cancellationToken);

    /// <summary>Opens (or reuses) a read-only tab for a generated metadata-as-source file and navigates to its identifier.</summary>
    Task<bool> OpenMetadataAsSourceAsync(MetadataAsSourceFile file, CancellationToken cancellationToken);
}

/// <summary>
/// Connects the VS-MEF navigation services to the app's UI layer, which lives in a separate
/// (MEF2) container and cannot be composed into the editor graph directly.
/// </summary>
[Export, Shared]
public sealed class NavigationBridge
{
    public INavigationHost? Host { get; set; }
}
