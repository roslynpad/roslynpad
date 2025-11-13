using Avalonia.Data;
using Microsoft.CodeAnalysis.CodeActions;
using RoslynPad.Roslyn.CodeActions;
using RoslynPad.Roslyn.CodeFixes;

namespace RoslynPad.Editor;

internal class ContextActionsBulbContextMenu : MenuFlyout
{
    private static readonly CodeActionTitleConverter s_titleConverter = new();
    private static readonly CodeActionsConverter s_actionsConverter = new();
    
    private readonly ActionCommandConverter _converter;

    public ContextActionsBulbContextMenu(ActionCommandConverter converter)
    {
        _converter = converter;
        Placement = PlacementMode.Right;
    }

    private Style CreateItemContainerStyle() => new(s => s.OfType<MenuItem>())
    {
        Setters =
        {
            new Setter(MenuItem.HeaderProperty, new Binding { Converter = s_titleConverter }),
            new Setter(MenuItem.ItemsSourceProperty, new Binding { Converter = s_actionsConverter }),
            new Setter(MenuItem.CommandProperty, new Binding { Converter = _converter })
        }
    };

    protected override Control CreatePresenter()
    {
        var presenter = base.CreatePresenter();
        presenter.Styles.Add(CreateItemContainerStyle());
        return presenter;
    }
}

internal sealed class CodeActionTitleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value switch
        {
            CodeAction codeAction => codeAction.Title,
            CodeFix codeFix => codeFix.Action.Title,
            _ => value?.ToString()
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

internal sealed class CodeActionsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value switch
        {
            CodeAction codeAction when codeAction.HasCodeActions() => codeAction.GetCodeActions(),
            CodeFix codeFix when codeFix.Action.HasCodeActions() => codeFix.Action.GetCodeActions(),
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
