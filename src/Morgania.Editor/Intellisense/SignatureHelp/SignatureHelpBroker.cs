#nullable enable

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation;

using System.Collections.ObjectModel;
using System.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// The signature help broker. The vendored repo carries only the Def contracts for signature
/// help (the VS broker was never open-sourced), so this implementation is Morgania-authored
/// from the contract documentation: sessions augment their signature list
/// from ordered, content-type-scoped <see cref="ISignatureHelpSourceProvider"/> exports and
/// present through the ordered <see cref="IIntellisensePresenterProvider"/> chain.
/// </summary>
[Export(typeof(ISignatureHelpBroker))]
[Shared]
public sealed class SignatureHelpBroker : ISignatureHelpBroker
{
    /// <summary>
    /// The base content type for signature help text. Languages register derived content types
    /// named "{content type} Signature Help" (e.g. Roslyn's "CSharp Signature Help") and export
    /// classifiers for them; the default presenter classifies signature content through them.
    /// In VS this definition lives in the closed-source intellisense implementation.
    /// </summary>
    [Export]
    [Name("sighelp")]
    [BaseDefinition("text")]
    public ContentTypeDefinition? SignatureHelpContentTypeDefinition { get; set; }

    private readonly List<Lazy<ISignatureHelpSourceProvider, OrderableContentTypeMetadata>> _sourceProviders;
    private readonly List<Lazy<IIntellisensePresenterProvider, OrderableContentTypeMetadata>> _presenterProviders;

    [ImportingConstructor]
    public SignatureHelpBroker(
        [ImportMany] IEnumerable<Lazy<ISignatureHelpSourceProvider, OrderableContentTypeMetadata>> sourceProviders,
        [ImportMany] IEnumerable<Lazy<IIntellisensePresenterProvider, OrderableContentTypeMetadata>> presenterProviders)
    {
        _sourceProviders = Orderer.Order(sourceProviders.ToList()).ToList();
        _presenterProviders = Orderer.Order(presenterProviders.ToList()).ToList();
    }

    public ISignatureHelpSession? TriggerSignatureHelp(ITextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        var caret = textView.Caret.Position.BufferPosition;
        return TriggerSignatureHelp(
            textView,
            caret.Snapshot.CreateTrackingPoint(caret.Position, PointTrackingMode.Negative),
            trackCaret: true);
    }

    public ISignatureHelpSession? TriggerSignatureHelp(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret)
    {
        var session = CreateSignatureHelpSession(textView, triggerPoint, trackCaret);
        session.Start();
        return session.IsDismissed ? null : session;
    }

    public ISignatureHelpSession CreateSignatureHelpSession(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret)
    {
        ArgumentNullException.ThrowIfNull(textView);
        ArgumentNullException.ThrowIfNull(triggerPoint);
        return new SignatureHelpSession(this, textView, triggerPoint, trackCaret);
    }

    public void DismissAllSessions(ITextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        foreach (var session in GetSessionList(textView).ToArray())
        {
            session.Dismiss();
        }
    }

    public bool IsSignatureHelpActive(ITextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        return GetSessionList(textView).Count > 0;
    }

    public ReadOnlyCollection<ISignatureHelpSession> GetSessions(ITextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        return new ReadOnlyCollection<ISignatureHelpSession>([.. GetSessionList(textView)]);
    }

    internal static List<ISignatureHelpSession> GetSessionList(ITextView textView)
        => textView.Properties.GetOrCreateSingletonProperty(
            typeof(SignatureHelpBroker),
            static () => new List<ISignatureHelpSession>());

    internal List<ISignatureHelpSource> CreateSources(ITextBuffer buffer)
    {
        var sources = new List<ISignatureHelpSource>();
        var contentType = buffer.ContentType;
        foreach (var provider in _sourceProviders)
        {
            bool contentTypeMatches = provider.Metadata.ContentTypes?.Any(contentType.IsOfType) != false;
            if (contentTypeMatches && provider.Value.TryCreateSignatureHelpSource(buffer) is { } source)
            {
                sources.Add(source);
            }
        }

        return sources;
    }

    internal IIntellisensePresenter? CreatePresenter(IIntellisenseSession session)
    {
        var contentType = session.TextView.TextBuffer.ContentType;
        foreach (var provider in _presenterProviders)
        {
            bool contentTypeMatches = provider.Metadata.ContentTypes?.Any(contentType.IsOfType) != false;
            if (contentTypeMatches && provider.Value.TryCreateIntellisensePresenter(session) is { } presenter)
            {
                return presenter;
            }
        }

        return null;
    }
}
