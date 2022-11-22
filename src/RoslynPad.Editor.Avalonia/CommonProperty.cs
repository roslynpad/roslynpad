using Avalonia;
using System;

namespace RoslynPad.Editor;

public static class CommonProperty
{
    public static StyledProperty<TValue> Register<TOwner, TValue>(string name,
        TValue defaultValue = default!, PropertyOptions options = PropertyOptions.None,
        Action<TOwner, CommonPropertyChangedArgs<TValue>>? onChanged = null)
        where TOwner : AvaloniaObject
    {
        var property = AvaloniaProperty.Register<TOwner, TValue>(name, defaultValue!,
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
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                (o, e) => onChangedLocal(o, new CommonPropertyChangedArgs<TValue>((TValue)e.OldValue, (TValue)e.NewValue)));
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8604 // Possible null reference argument.
        }

        return property;
    }

    private static readonly Action<AvaloniaProperty[]> AffectsRender = ReflectionUtil.CreateDelegate<Action<AvaloniaProperty[]>>(typeof(Visual), nameof(AffectsRender));
    private static readonly Action<AvaloniaProperty[]> AffectsArrange = ReflectionUtil.CreateDelegate<Action<AvaloniaProperty[]>>(typeof(Visual), nameof(AffectsArrange));
    private static readonly Action<AvaloniaProperty[]> AffectsMeasure = ReflectionUtil.CreateDelegate<Action<AvaloniaProperty[]>>(typeof(Visual), nameof(AffectsMeasure));
}
