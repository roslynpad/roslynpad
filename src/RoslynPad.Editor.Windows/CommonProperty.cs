using System;
using System.Windows;

namespace RoslynPad.Editor
{
    public static class CommonProperty
    {
        public static StyledProperty<TValue> Register<TOwner, TValue>(string name,
            TValue defaultValue = default, PropertyOptions options = PropertyOptions.None,
            Action<TOwner, CommonPropertyChangedArgs<TValue>>? onChanged = null)
            where TOwner : DependencyObject
        {
            var metadataOptions = FrameworkPropertyMetadataOptions.None;

            if (options.Has(PropertyOptions.AffectsRender))
            {
                metadataOptions |= FrameworkPropertyMetadataOptions.AffectsRender;
            }

            if (options.Has(PropertyOptions.AffectsArrange))
            {
                metadataOptions |= FrameworkPropertyMetadataOptions.AffectsArrange;
            }

            if (options.Has(PropertyOptions.AffectsMeasure))
            {
                metadataOptions |= FrameworkPropertyMetadataOptions.AffectsMeasure;
            }

            if (options.Has(PropertyOptions.Inherits))
            {
                metadataOptions |= FrameworkPropertyMetadataOptions.Inherits;
            }

            if (options.Has(PropertyOptions.BindsTwoWay))
            {
                metadataOptions |= FrameworkPropertyMetadataOptions.BindsTwoWayByDefault;
            }

            var changedCallback = onChanged != null
                ? new PropertyChangedCallback((o, e) => onChanged((TOwner)o, new CommonPropertyChangedArgs<TValue>((TValue)e.OldValue, (TValue)e.NewValue)))
                : null;
            var metadata = new FrameworkPropertyMetadata(defaultValue, metadataOptions, changedCallback);
            var property = DependencyProperty.Register(name, typeof(TValue), typeof(TOwner), metadata);

            return new StyledProperty<TValue>(property);
        }

        public static TValue GetValue<TValue>(this DependencyObject o, StyledProperty<TValue> property)
        {
            return (TValue)o.GetValue(property.Property);
        }

        public static void SetValue<TValue>(this DependencyObject o, StyledProperty<TValue> property, TValue value)
        {
            o.SetValue(property.Property, value);
        }
    }

    public sealed class StyledProperty<TValue>
    {
        public DependencyProperty Property { get; }

        public StyledProperty(DependencyProperty property)
        {
            Property = property;
        }

        public StyledProperty<TValue> AddOwner<TOwner>() =>
            new StyledProperty<TValue>(Property.AddOwner(typeof(TOwner)));

        public Type PropertyType => Property.PropertyType;
    }
}