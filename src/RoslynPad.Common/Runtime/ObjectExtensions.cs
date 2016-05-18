using System;

namespace RoslynPad.Runtime
{
    public static class ObjectExtensions
    {
        public static T Dump<T>(this T o, string header = null)
        {
            Dumped?.Invoke(o, header);
            return o;
        }
        
        internal static event Action<object, string> Dumped;
    }
}
