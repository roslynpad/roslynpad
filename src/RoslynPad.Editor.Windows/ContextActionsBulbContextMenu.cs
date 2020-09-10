using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace RoslynPad.Editor
{
    internal class ContextActionsBulbContextMenu : ContextMenu
    {
        private readonly ActionCommandConverter _converter;

        public ContextActionsBulbContextMenu(ActionCommandConverter converter)
        {
            _converter = converter;
            ItemContainerStyle = CreateItemContainerStyle();
            HasDropShadow = false;
            Placement = SystemParameters.MenuDropAlignment ? PlacementMode.Left : PlacementMode.Right;
        }

        private Style CreateItemContainerStyle()
        {
            var style = new Style(typeof(MenuItem), TryFindResource(typeof(MenuItem)) as Style);
            style.Setters.Add(new Setter(MenuItem.CommandProperty,
                new Binding { Converter = _converter }));
            style.Seal();
            return style;
        }
    }
}
