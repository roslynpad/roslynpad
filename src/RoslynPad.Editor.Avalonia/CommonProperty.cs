namespace RoslynPad.Editor;

#pragma warning disable AVP1001 // The same AvaloniaProperty should not be registered twice

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

        if (typeof(Visual).IsAssignableFrom(typeof(TOwner)) && options.Has(PropertyOptions.AffectsRender))
        {
            AffectsRender([property]);
        }

        if (typeof(Layoutable).IsAssignableFrom(typeof(TOwner)))
        {
            if (options.Has(PropertyOptions.AffectsArrange))
            {
                AffectsArrange([property]);
            }

            if (options.Has(PropertyOptions.AffectsMeasure))
            {
                AffectsMeasure([property]);
            }
        }

        var onChangedLocal = onChanged;
        if (onChangedLocal != null)
        {
            property.Changed.AddClassHandler<TOwner>(
                (o, e) => onChangedLocal(o, new CommonPropertyChangedArgs<TValue>((TValue)e.OldValue!, (TValue)e.NewValue!)));
        }

        return property;
    }

    private static Action<AvaloniaProperty[]> AffectsRender { get; } = ReflectionUtil.CreateDelegate<Visual, Action<AvaloniaProperty[]>>(nameof(AffectsRender));
    private static Action<AvaloniaProperty[]> AffectsArrange { get; } = ReflectionUtil.CreateDelegate<Layoutable, Action<AvaloniaProperty[]>>(nameof(AffectsArrange));
    private static Action<AvaloniaProperty[]> AffectsMeasure { get; } = ReflectionUtil.CreateDelegate<Layoutable, Action<AvaloniaProperty[]>>(nameof(AffectsMeasure));
}
