using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;

namespace RoslynPad;

internal static class BindingHelpers
{
    public static IEnumerable<Inline> GetInlines(DependencyObject obj) => (IEnumerable<Inline>)obj.GetValue(InlinesProperty);

    public static void SetInlines(DependencyObject obj, IEnumerable<Inline> value) => obj.SetValue(InlinesProperty, value);

    public static readonly DependencyProperty InlinesProperty =
        DependencyProperty.RegisterAttached("Inlines", typeof(IEnumerable<Inline>), typeof(BindingHelpers),
            new FrameworkPropertyMetadata(OnInlinesChanged));

    private static void OnInlinesChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is not TextBlock textBlock)
        {
            return;
        }

        textBlock.Inlines.Clear();

        if (e.NewValue != null)
        {
            textBlock.Inlines.AddRange((IEnumerable<Inline>)e.NewValue);
        }
    }
}
