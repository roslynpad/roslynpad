namespace RoslynPad.Utilities
{
    internal static class ReflectionExtensions
    {
        public static T GetPropertyValue<T>(this object o, string name)
        {
            return (T)o.GetType().GetProperty(name).GetValue(o);
        }
    }
}