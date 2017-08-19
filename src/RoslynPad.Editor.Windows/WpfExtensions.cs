using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace RoslynPad.Editor
{
    public static class WpfExtensions
    {
        public static Window GetWindow(this UIElement o) => Window.GetWindow(o);

        public static Dispatcher GetDispatcher(this DispatcherObject o) => o.Dispatcher;

        public static Size GetRenderSize(this UIElement element) => element.RenderSize;

        public static void HookupLoadedUnloadedAction(this FrameworkElement element, Action<bool> action)
        {
            if (element.IsLoaded)
            {
                action(true);
            }

            element.Loaded += (o, e) => action(true);
            element.Unloaded += (o, e) => action(false);
        }

        public static void AttachLocationChanged(this Window topLevel, EventHandler handler)
        {
            topLevel.LocationChanged += handler;
        }

        public static void DetachLocationChanged(this Window topLevel, EventHandler handler)
        {
            topLevel.LocationChanged -= handler;
        }

        public static T AsFrozen<T>(this T freezable) where T : Freezable
        {
            freezable.Freeze();
            return freezable;
        }

        public static void BeginFigure(this StreamGeometryContext context, Point point, bool isFilled)
        {
            context.BeginFigure(point, isFilled, isClosed: false);
        }

        public static void SetBorderThickness(this Control control, double thickness)
        {
            control.BorderThickness = new Thickness(thickness);
        }

        public static bool HasModifiers(this KeyEventArgs args, ModifierKeys modifier) =>
            (args.KeyboardDevice.Modifiers & modifier) == modifier;

        public static void Open(this ToolTip toolTip, FrameworkElement control) => toolTip.IsOpen = true;
        public static void Close(this ToolTip toolTip, FrameworkElement control) => toolTip.IsOpen = false;
        public static void SetContent(this ToolTip toolTip, Control control, object content) => toolTip.Content = content;

        public static void SetItems(this ItemsControl itemsControl, System.Collections.IEnumerable enumerable) => 
            itemsControl.ItemsSource = enumerable;
    }
}