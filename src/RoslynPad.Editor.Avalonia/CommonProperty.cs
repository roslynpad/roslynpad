using Avalonia;
using System;
using RoslynPad.Utilities;

namespace RoslynPad.Editor
{
    public static class CommonProperty
    {
        public static StyledProperty<TValue> Register<TOwner, TValue>(string name,
            TValue defaultValue = default, PropertyOptions options = PropertyOptions.None,
            Action<TOwner, CommonPropertyChangedArgs<TValue>>? onChanged = null)
            where TOwner : AvaloniaObject
        {
            var property = AvaloniaProperty.Register<TOwner, TValue>(name, defaultValue,
                options.Has(PropertyOptions.Inherits),
                options.Has(PropertyOptions.BindsTwoWay)
                    ? Avalonia.Data.BindingMode.TwoWay
                    : Avalonia.Data.BindingMode.OneWay);

            if (options.Has(PropertyOptions.AffectsRender))
            {
                AffectsRender(new[] { property });
            }

            if (options.Has(PropertyOptions.AffectsArrange))
            {
                AffectsArrange(new[] { property });
            }

            if (options.Has(PropertyOptions.AffectsMeasure))
            {
                AffectsMeasure(new[] { property });
            }

            var onChangedLocal = onChanged;
            if (onChangedLocal != null)
            {
                property.Changed.AddClassHandler<TOwner>(
                    (o, e) => onChangedLocal(o, new CommonPropertyChangedArgs<TValue>((TValue)e.OldValue, (TValue)e.NewValue)));
            }

            return property;
        }

        private static Action<AvaloniaProperty[]> AffectsRender = ReflectionUtil.CreateDelegate<Action<AvaloniaProperty[]>>(typeof(Visual), nameof(AffectsRender));
        private static Action<AvaloniaProperty[]> AffectsArrange = ReflectionUtil.CreateDelegate<Action<AvaloniaProperty[]>>(typeof(Visual), nameof(AffectsArrange));
        private static Action<AvaloniaProperty[]> AffectsMeasure = ReflectionUtil.CreateDelegate<Action<AvaloniaProperty[]>>(typeof(Visual), nameof(AffectsMeasure));
    }
}