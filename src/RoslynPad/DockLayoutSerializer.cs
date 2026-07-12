using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Avalonia.Collections;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Avalonia.Core;
using Dock.Model.Controls;
using Dock.Model.Core;

namespace RoslynPad;

/// <summary>
/// Serializes the dock layout for persistence in the application settings.
/// Based on <c>Dock.Model.Avalonia.Json.AvaloniaDockSerializer</c> (MIT,
/// https://github.com/wieslawsoltes/Dock), rewritten over a source-generated context
/// for AOT compatibility, and fixed to deserialize dockable lists as observable
/// <see cref="AvaloniaList{T}"/> so the dock UI tracks later changes (an upstream TODO:
/// with plain lists, tabs added after a layout restore never show up).
/// </summary>
internal static class DockLayoutSerializer
{
    private static readonly string[] s_dockableProperties =
    [
        "Id", "Title", "Context", "Owner", "OriginalOwner", "CanClose", "CanPin", "KeepPinnedDockableVisible",
        "PinnedDockDisplayModeOverride", "PinnedBounds", "CanFloat", "CanDrag", "CanDrop", "CanDockAsDocument", "DockingState",
    ];

    private static readonly string[] s_dockProperties =
    [
        .. s_dockableProperties,
        "VisibleDockables", "ActiveDockable", "DefaultDockable", "FocusedDockable", "Proportion",
        "Dock", "IsActive", "IsEmpty", "IsCollapsable", "CanCloseLastDockable",
    ];

    private static readonly string[] s_rootDockProperties =
    [
        .. s_dockProperties,
        "IsFocusableRoot", "HiddenDockables", "LeftPinnedDockables", "RightPinnedDockables", "TopPinnedDockables",
        "BottomPinnedDockables", "PinnedDock", "PinnedDockDisplayMode", "Window", "Windows", "FloatingWindowHostMode",
    ];

    private static readonly string[] s_windowProperties =
    [
        "Id", "X", "Y", "Width", "Height", "WindowState", "Topmost", "Title", "OwnerMode", "ParentWindow", "IsModal", "ShowInTaskbar", "Layout",
    ];

    private static readonly Dictionary<Type, string[]> s_serializedProperties = new()
    {
        [typeof(DockableBase)] = s_dockableProperties,
        [typeof(IDockable)] = s_dockableProperties,
        [typeof(Document)] = s_dockableProperties,
        [typeof(IDocument)] = s_dockableProperties,
        [typeof(IDocumentContent)] = s_dockableProperties,
        [typeof(Tool)] = s_dockableProperties,
        [typeof(ITool)] = s_dockableProperties,
        [typeof(IToolContent)] = s_dockableProperties,
        [typeof(DockRect)] = ["X", "Y", "Width", "Height"],
        [typeof(DockBase)] = s_dockProperties,
        [typeof(IDock)] = s_dockProperties,
        [typeof(DockDock)] = [.. s_dockProperties, "LastChildFill"],
        [typeof(IDockDock)] = [.. s_dockProperties, "LastChildFill"],
        [typeof(DocumentDock)] = [.. s_dockProperties, "CanCreateDocument", "CanUpdateItemsSourceOnUnregister"],
        [typeof(IDocumentDock)] = [.. s_dockProperties, "CanCreateDocument"],
        [typeof(IDocumentDockContent)] = s_dockProperties,
        [typeof(ProportionalDock)] = [.. s_dockProperties, "Orientation"],
        [typeof(IProportionalDock)] = [.. s_dockProperties, "Orientation"],
        [typeof(ProportionalDockSplitter)] = s_dockProperties[..^1],
        [typeof(IProportionalDockSplitter)] = s_dockProperties,
        [typeof(RootDock)] = s_rootDockProperties,
        [typeof(IRootDock)] = s_rootDockProperties,
        [typeof(ToolDock)] = [.. s_dockProperties, "Alignment", "IsExpanded", "AutoHide", "GripMode", "CanUpdateItemsSourceOnUnregister"],
        [typeof(IToolDock)] = [.. s_dockProperties, "Alignment", "IsExpanded", "AutoHide", "GripMode"],
        [typeof(DockWindow)] = s_windowProperties,
        [typeof(IDockWindow)] = s_windowProperties,
    };

    private static readonly JsonSerializerOptions s_options = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve,
        IgnoreReadOnlyProperties = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        TypeInfoResolver = DockLayoutContext.Default.WithAddedModifier(Customize),
    };

    private static readonly JsonTypeInfo<RootDock> s_rootDockTypeInfo = (JsonTypeInfo<RootDock>)s_options.GetTypeInfo(typeof(RootDock));

    public static string Serialize(RootDock layout) => JsonSerializer.Serialize(layout, s_rootDockTypeInfo);

    public static RootDock? Deserialize(string json) => JsonSerializer.Deserialize(json, s_rootDockTypeInfo);

    private static void Customize(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind == JsonTypeInfoKind.Object)
        {
            // Only the whitelisted properties are persisted; everything else — most
            // notably the ones inherited from StyledElement (DataContext, Resources, ...),
            // which can't carry [JsonIgnore] — is stripped.
            var names = s_serializedProperties.GetValueOrDefault(typeInfo.Type);
            for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
            {
                if (names?.Contains(typeInfo.Properties[i].Name) != true)
                {
                    typeInfo.Properties.RemoveAt(i);
                }
            }

            typeInfo.PolymorphismOptions = CreatePolymorphismOptions(typeInfo.Type);
        }
        else if (typeInfo.Type == typeof(IList<IDockable>))
        {
            typeInfo.CreateObject = static () => new AvaloniaList<IDockable>();
        }
        else if (typeInfo.Type == typeof(IList<IDockWindow>))
        {
            typeInfo.CreateObject = static () => new AvaloniaList<IDockWindow>();
        }
    }

    private static JsonPolymorphismOptions? CreatePolymorphismOptions(Type type)
    {
        (Type Type, string Discriminator)[]? derivedTypes = null;
        var unknownHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType;

        if (type == typeof(IDockable) || type == typeof(DockableBase))
        {
            derivedTypes =
            [
                (typeof(Document), "Document"), (typeof(Tool), "Tool"), (typeof(DockDock), "DockDock"),
                (typeof(DocumentDock), "DocumentDock"), (typeof(ProportionalDock), "ProportionalDock"),
                (typeof(ProportionalDockSplitter), "ProportionalDockSplitter"), (typeof(RootDock), "RootDock"), (typeof(ToolDock), "ToolDock"),
            ];
        }
        else if (type == typeof(IDock) || type == typeof(DockBase))
        {
            derivedTypes =
            [
                (typeof(DockDock), "DockDock"), (typeof(DocumentDock), "DocumentDock"), (typeof(ProportionalDock), "ProportionalDock"),
                (typeof(ProportionalDockSplitter), "ProportionalDockSplitter"), (typeof(RootDock), "RootDock"), (typeof(ToolDock), "ToolDock"),
            ];
        }
        else if (type == typeof(IToolDock) || type == typeof(ToolDock))
        {
            derivedTypes = [(typeof(ToolDock), "ToolDock")];
        }
        else if (type == typeof(IRootDock) || type == typeof(RootDock))
        {
            derivedTypes = [(typeof(RootDock), "RootDock")];
            unknownHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor;
        }
        else if (type == typeof(IDockWindow) || type == typeof(DockWindow))
        {
            derivedTypes = [(typeof(DockWindow), "DockWindow")];
        }

        if (derivedTypes is null)
        {
            return null;
        }

        var options = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "$type",
            UnknownDerivedTypeHandling = unknownHandling,
            IgnoreUnrecognizedTypeDiscriminators = true,
        };

        foreach (var (derivedType, discriminator) in derivedTypes)
        {
            options.DerivedTypes.Add(new JsonDerivedType(derivedType, discriminator));
        }

        return options;
    }
}

[JsonSerializable(typeof(RootDock))]
[JsonSerializable(typeof(ProportionalDock))]
[JsonSerializable(typeof(ProportionalDockSplitter))]
[JsonSerializable(typeof(DocumentDock))]
[JsonSerializable(typeof(ToolDock))]
[JsonSerializable(typeof(DockDock))]
[JsonSerializable(typeof(Document))]
[JsonSerializable(typeof(Tool))]
[JsonSerializable(typeof(DockWindow))]
internal partial class DockLayoutContext : JsonSerializerContext;
