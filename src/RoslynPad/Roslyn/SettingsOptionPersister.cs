using System.Composition;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using RoslynPad.UI;

namespace RoslynPad.Roslyn;

/// <summary>
/// Persists Roslyn global options (editor UI state, e.g. the inline diagnostics toggle) into the
/// untyped "roslyn" object of the app settings file — RoslynPad's equivalent of the VS settings
/// store. <see cref="IGlobalOptionService"/> consults persisters on the first read of each option
/// and on every set. The app settings live in the separate MEF2 container, so <c>MainViewModel</c>
/// registers them here once the Roslyn host is up; fetches before that return defaults.
/// </summary>
[Export(typeof(IOptionPersisterProvider)), Export, Shared]
public sealed class SettingsOptionPersister : IOptionPersisterProvider, IOptionPersister
{
    public IApplicationSettingsValues? Settings { get; set; }

    IOptionPersister IOptionPersisterProvider.GetOrCreatePersister() => this;

    bool IOptionPersister.TryFetch(OptionKey2 optionKey, out object? value)
    {
        value = null;
        return IsSupported(optionKey) &&
            Settings?.Roslyn?[optionKey.Option.Definition.ConfigName] is JsonValue serialized &&
            optionKey.Option.Definition.Serializer.TryParse(serialized.ToString(), out value);
    }

    bool IOptionPersister.TryPersist(OptionKey2 optionKey, object? value)
    {
        if (Settings is not { } settings || !IsSupported(optionKey))
        {
            return false;
        }

        var roslyn = settings.Roslyn?.DeepClone().AsObject() ?? new JsonObject();
        roslyn[optionKey.Option.Definition.ConfigName] = optionKey.Option.Definition.Serializer.Serialize(value);
        settings.Roslyn = roslyn; // reassigned (not mutated) so the settings file is saved
        return true;
    }

    // the app hosts C# only, so per-language options are stored unqualified
    private static bool IsSupported(OptionKey2 optionKey) => optionKey.Language is null or LanguageNames.CSharp;
}
