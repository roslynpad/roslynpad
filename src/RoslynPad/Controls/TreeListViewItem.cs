using System.Windows;
using System.Windows.Controls;

namespace RoslynPad.Controls;

internal class TreeListViewItem : TreeViewItem
{
    private static readonly DependencyPropertyKey LevelPropertyKey = DependencyProperty.RegisterReadOnly(
        "Level", typeof(int), typeof(TreeListViewItem), new FrameworkPropertyMetadata());

    public static readonly DependencyProperty LevelProperty = LevelPropertyKey.DependencyProperty;

    public int Level => (int)GetValue(LevelProperty);

    internal static readonly DependencyPropertyKey ShowSeparatorPropertyKey = DependencyProperty.RegisterReadOnly(
        "ShowSeparator", typeof(bool), typeof(TreeListViewItem), new FrameworkPropertyMetadata());

    public static readonly DependencyProperty ShowSeparatorProperty = ShowSeparatorPropertyKey.DependencyProperty;

    public bool ShowSeparator => (bool)GetValue(ShowSeparatorProperty);

    protected override DependencyObject GetContainerForItemOverride() => new TreeListViewItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is TreeListViewItem;

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        element.SetValue(LevelPropertyKey, Level + 1);
    }
}
