using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace RoslynPad.Editor
{
    internal static class AvaloniaExtensions
    {
        public static T FindAncestorByType<T>(this IControl control)
            where T : IControl
        {
            while (control != null && !(control is T))
            {
                control = control.Parent;
            }

            return (T)control!;
        }

        public static Window? GetWindow(this Control c) => c.FindAncestorByType<Window>();

        public static Dispatcher GetDispatcher(this IControl o) => Dispatcher.UIThread;

        public static Size GetRenderSize(this IControl element) => element.Bounds.Size;

        public static void HookupLoadedUnloadedAction(this IControl element, Action<bool> action)
        {
            if (element.IsAttachedToVisualTree)
            {
                action(true);
            }

            element.AttachedToVisualTree += (o, e) => action(true);
            element.DetachedFromVisualTree += (o, e) => action(false);
        }

        public static void AttachLocationChanged(this Window topLevel, EventHandler<PixelPointEventArgs> handler)
        {
            topLevel.PositionChanged += handler;
        }

        public static void DetachLocationChanged(this Window topLevel, EventHandler<PixelPointEventArgs> handler)
        {
            topLevel.PositionChanged -= handler;
        }

        public static IBrush AsFrozen(this IMutableBrush freezable)
        {
            return freezable.ToImmutable();
        }

        public static void Freeze(this Pen pen)
        {
            // nop
        }

        public static void Freeze(this Geometry geometry)
        {
            // nop
        }

        public static void PolyLineTo(this StreamGeometryContext context, IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            foreach (var point in points)
            {
                context.LineTo(point);
            }
        }

        public static void SetBorderThickness(this TemplatedControl control, double thickness)
        {
            control.BorderThickness = new Thickness(thickness);
        }

        public static void Close(this PopupRoot window) => window.Hide();

        public static bool HasModifiers(this KeyEventArgs args, KeyModifiers modifier) =>
            (args.KeyModifiers & modifier) == modifier;

        public static void Open(this ToolTip toolTip, Control control) => ToolTip.SetIsOpen(control, true);

        public static void Close(this ToolTip toolTip, Control control) => ToolTip.SetIsOpen(control, false);

        public static void SetContent(this ToolTip toolTip, Control control, object content) => ToolTip.SetTip(control, content);

        public static void SetItems(this ItemsControl itemsControl, System.Collections.IEnumerable enumerable) =>
            itemsControl.Items = enumerable;
    }
}