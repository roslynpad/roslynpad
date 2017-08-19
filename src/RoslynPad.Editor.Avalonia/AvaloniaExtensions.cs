using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using RoslynPad.Utilities;
using System;
using System.Collections.Generic;

namespace RoslynPad.Editor
{
    public static class AvaloniaExtensions
    {
        public static T FindAncestorByType<T>(this IControl control)
            where T : IControl
        {
            while (control != null && !(control is T))
            {
                control = control.Parent;
            }

            return (T)control;
        }

        public static Window GetWindow(this Control c) => c.FindAncestorByType<Window>();

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

        public static void AttachLocationChanged(this Window topLevel, EventHandler<PointEventArgs> handler)
        {
            topLevel.PositionChanged += handler;
        }

        public static void DetachLocationChanged(this Window topLevel, EventHandler<PointEventArgs> handler)
        {
            topLevel.PositionChanged -= handler;
        }

        public static IBrush AsFrozen(this IMutableBrush freezable)
        {
            return freezable.ToImmutable();
        }

        // nop
        public static void Freeze(this Pen pen) { }

        // nop
        public static void Freeze(this Geometry geometry) { }

        public static void PolyLineTo(this StreamGeometryContext context, IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            foreach (var point in points)
            {
                context.LineTo(point);
            }
        }

        public static void SetBorderThickness(this TemplatedControl control, double thickness)
        {
            control.BorderThickness = thickness;
        }

        public static void Close(this PopupRoot window) => window.Hide();

        public static bool HasModifiers(this KeyEventArgs args, InputModifiers modifier) =>
            (args.Modifiers & modifier) == modifier;

        // workaround for Avalonia missing a settable ToolTip.IsOpen property
        private static Action<object, PointerEventArgs> ToolTipOpen = ReflectionUtil.CreateDelegate<Action<object, PointerEventArgs>>(typeof(ToolTip), "ControlPointerEnter");
        private static Action<object, PointerEventArgs> ToolTipClose = ReflectionUtil.CreateDelegate<Action<object, PointerEventArgs>>(typeof(ToolTip), "ControlPointerLeave");

        public static void Open(this ToolTip toolTip, IControl control)
        {
            ToolTipClose(control, null);
            ToolTipOpen(control, null);
        }

        public static void Close(this ToolTip toolTip, IControl control) => ToolTipClose(control, null);

        public static void SetContent(this ToolTip toolTip, Control control, object content) => ToolTip.SetTip(control, content);

        public static void SetItems(this ItemsControl itemsControl, System.Collections.IEnumerable enumerable) =>
            itemsControl.Items = enumerable;
    }
}