using System;

namespace RoslynPad.Editor
{
    [Flags]
    public enum PropertyOptions
    {
        None,
        AffectsRender  = 1,
        AffectsArrange = 2,
        AffectsMeasure = 4,
        BindsTwoWay    = 8,
        Inherits       = 16,
    }

    public static class ProeprtyExtensions
    {
        public static bool Has(this PropertyOptions options, PropertyOptions value) =>
            (options & value) == value;
    }

    public class CommonPropertyChangedArgs<T>
    {
        public T OldValue { get; }

        public T NewValue { get; }

        public CommonPropertyChangedArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}