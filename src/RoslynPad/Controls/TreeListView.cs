using System.Windows;
using System.Windows.Controls;
using RoslynPad.Editor;

namespace RoslynPad.Controls;

internal class TreeListView : TreeView
{
    public static readonly DependencyProperty ShowSeparatorProperty = DependencyProperty.Register(
        "ShowSeparator", typeof(bool), typeof(TreeListView), new FrameworkPropertyMetadata(true));

    public bool ShowSeparator
    {
        get => (bool)GetValue(ShowSeparatorProperty);
        set => SetValue(ShowSeparatorProperty, value);
    }

    protected override DependencyObject GetContainerForItemOverride() => new TreeListViewItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is TreeListViewItem;

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        element.SetValue(TreeListViewItem.ShowSeparatorPropertyKey, ShowSeparator);
    }

    private GridViewColumnCollection? _columns;
    public GridViewColumnCollection Columns => _columns ??= [];
}
