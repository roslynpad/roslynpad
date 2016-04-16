using System;

namespace RoslynPad.Runtime
{
    public static class ObjectExtensions
    {
        public static T Dump<T>(this T o)
        {
            Dumped?.Invoke(o, DumpTarget.Text);
            return o;
        }

        [Obsolete("Use Dump")]
        public static T DumpToPropertyGrid<T>(this T o)
        {
            Dumped?.Invoke(o, DumpTarget.PropertyGrid);
            return o;
        }

        internal static event Action<object, DumpTarget> Dumped;
    }

    internal enum DumpTarget
    {
        Text,
        PropertyGrid,
    }
}
