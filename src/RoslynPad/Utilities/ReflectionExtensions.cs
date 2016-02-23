namespace RoslynPad.Utilities
{
    internal static class ReflectionExtensions
    {
        public static T GetFieldValue<T>(this object o, string name)
        {
            return (T)o.GetType().GetField(name).GetValue(o);
        }

        public static T GetPropertyValue<T>(this object o, string name)
        {
            return (T)o.GetType().GetProperty(name).GetValue(o);
        }
    }
}