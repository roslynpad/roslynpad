// WPF compatibility shims: Roslyn EditorFeatures source references System.Windows.* types,
// which are mapped onto their Avalonia equivalents so the source compiles unmodified against
// the Morgania (Avalonia) editor.
//
// - The empty System.Windows.* namespaces satisfy `using` directives in upstream files.
// - The global using aliases map WPF type names to Avalonia types.
// - WpfCompatExtensions bridges API-shape differences (extension members, C# 14).

global using Border = Avalonia.Controls.Border;
global using Brush = Avalonia.Media.Brush;
global using Canvas = Avalonia.Controls.Canvas;
global using Color = Avalonia.Media.Color;
global using DependencyObject = Avalonia.AvaloniaObject;
global using DispatcherPriority = Avalonia.Threading.DispatcherPriority;
global using FontStyles = Avalonia.Media.FontStyle;
global using FontWeights = Avalonia.Media.FontWeight;
global using Geometry = Avalonia.Media.Geometry;
global using Inline = Avalonia.Controls.Documents.Inline;
global using Line = Avalonia.Controls.Shapes.Line;
global using Run = Avalonia.Controls.Documents.Run;
global using SolidColorBrush = Avalonia.Media.SolidColorBrush;
global using TextBlock = Avalonia.Controls.TextBlock;
global using TextElement = Avalonia.Controls.Documents.TextElement;
global using TextTrimming = Avalonia.Media.TextTrimming;
global using TextWrapping = Avalonia.Media.TextWrapping;
global using Thickness = Avalonia.Thickness;
global using UIElement = Avalonia.Controls.Control;
global using UserControl = Avalonia.Controls.UserControl;

namespace System.Windows
{
    file sealed class Dummy;
}

namespace System.Windows.Controls
{
    file sealed class Dummy;
}

namespace System.Windows.Documents
{
    file sealed class Dummy;
}

namespace System.Windows.Media
{
    file sealed class Dummy;
}

namespace System.Windows.Shapes
{
    file sealed class Dummy;
}

namespace System.Windows.Threading
{
    file sealed class Dummy;
}

internal static class WpfCompatExtensions
{
    extension(Avalonia.Controls.Control control)
    {
        /// <summary>
        /// WPF exposes a per-element Dispatcher; Avalonia has a single UI-thread dispatcher.
        /// </summary>
        public Avalonia.Threading.Dispatcher Dispatcher => Avalonia.Threading.Dispatcher.UIThread;
    }

    extension(Avalonia.Controls.Shapes.Line line)
    {
        public double X1
        {
            get => line.StartPoint.X;
            set => line.StartPoint = new Avalonia.Point(value, line.StartPoint.Y);
        }

        public double Y1
        {
            get => line.StartPoint.Y;
            set => line.StartPoint = new Avalonia.Point(line.StartPoint.X, value);
        }

        public double X2
        {
            get => line.EndPoint.X;
            set => line.EndPoint = new Avalonia.Point(value, line.EndPoint.Y);
        }

        public double Y2
        {
            get => line.EndPoint.Y;
            set => line.EndPoint = new Avalonia.Point(line.EndPoint.X, value);
        }
    }

    extension(Avalonia.Controls.Shapes.Shape shape)
    {
        /// <summary>
        /// Avalonia always snaps to device pixels; setter is a no-op for WPF compatibility.
        /// </summary>
        public bool SnapsToDevicePixels
        {
            get => true;
            set { }
        }
    }

    /// <summary>
    /// WPF-style TryFindResource that returns the resource (or null) instead of a bool.
    /// </summary>
    public static object? TryFindResource(this Avalonia.Controls.Control control, object key)
        => Avalonia.Controls.ResourceNodeExtensions.TryFindResource(control, key, out var value) ? value : null;

    /// <summary>
    /// Stand-in for the WPF-specific JoinableTaskFactory.WithPriority; Avalonia rendering
    /// priority scheduling is not supported, so the factory is returned unchanged.
    /// </summary>
    public static Microsoft.VisualStudio.Threading.JoinableTaskFactory WithPriority(
        this Microsoft.VisualStudio.Threading.JoinableTaskFactory factory,
        Avalonia.Threading.Dispatcher dispatcher,
        Avalonia.Threading.DispatcherPriority priority)
        => factory;
}
