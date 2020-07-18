using Avalonia.Controls;
using Avalonia.Styling;
using System.Linq;
using Avalonia.Data;
using Avalonia.Controls.Primitives;
using System.Reflection;
using System;

namespace RoslynPad.Editor
{
    internal class ContextActionsBulbContextMenu : ContextMenu, IStyleable
    {
        private readonly ActionCommandConverter _converter;

        private bool _opened;

        public ContextActionsBulbContextMenu(ActionCommandConverter converter)
        {
            _converter = converter;
            Styles.Add(CreateItemContainerStyle());
        }

        Type IStyleable.StyleKey => typeof(ContextMenu);

        private Style CreateItemContainerStyle()
        {
            var style = new Style(s => s.OfType<MenuItem>());
            style.Setters.Add(new Setter(MenuItem.CommandProperty,
                new Binding { Converter = _converter }));
            return style;
        }

        public new void Open(Control control)
        {
            base.Open(control);

            // workaroud for Avalonia's lack of placement option
            if (!_opened)
            {
                _opened = true;

                if (typeof(ContextMenu).GetField("_popup", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(this) is Popup popup)
                {
                    popup.PlacementMode = PlacementMode.Right;
                }

                base.Close();
                base.Open(control);
            }
        }
    }
}
