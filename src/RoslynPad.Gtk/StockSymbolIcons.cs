using RoslynPad.Roslyn.Completion;

namespace RoslynPad.Gtk
{
    internal static class StockSymbolIcons
    {
        public static string GetGlyphIcon(this Glyph glyph)
        {
            // TODO: static modifier
            var icon = GetGlyphIconInternal(glyph);
            return icon != null ? "md-" + icon : string.Empty;
        }

        private static string GetGlyphIconInternal(Glyph glyph)
        {
            switch (glyph)
            {
                case Glyph.Assembly:
                    return "field";
                case Glyph.ClassPublic:
                    return "class";
                case Glyph.ClassProtected:
                    return "protected-class";
                case Glyph.ClassPrivate:
                    return "private-class";
                case Glyph.ClassInternal:
                    return "internal-class";
                case Glyph.ConstantPublic:
                    return "literal";
                case Glyph.ConstantProtected:
                    return "protected-literal";
                case Glyph.ConstantPrivate:
                    return "private-literal";
                case Glyph.ConstantInternal:
                    return "internal-literal";
                case Glyph.DelegatePublic:
                    return "delegate";
                case Glyph.DelegateProtected:
                    return "protected-delegate";
                case Glyph.DelegatePrivate:
                    return "private-delegate";
                case Glyph.DelegateInternal:
                    return "internal-delegate";
                case Glyph.EnumPublic:
                    return "enum";
                case Glyph.EnumProtected:
                    return "enum";
                case Glyph.EnumPrivate:
                    return "enum";
                case Glyph.EnumInternal:
                    return "enum";
                case Glyph.EnumMember:
                    return "field";
                case Glyph.EventPublic:
                    return "event";
                case Glyph.EventProtected:
                    return "protected-event";
                case Glyph.EventPrivate:
                    return "private-event";
                case Glyph.EventInternal:
                    return "internal-event";
                case Glyph.ExtensionMethodPublic:
                    return "method";
                case Glyph.ExtensionMethodProtected:
                    return "protected-method";
                case Glyph.ExtensionMethodPrivate:
                    return "private-method";
                case Glyph.ExtensionMethodInternal:
                    return "internal-method";
                case Glyph.FieldPublic:
                    return "field";
                case Glyph.FieldProtected:
                    return "protected-field";
                case Glyph.FieldPrivate:
                    return "private-field";
                case Glyph.FieldInternal:
                    return "internal-field";
                case Glyph.InterfacePublic:
                    return "interface";
                case Glyph.InterfaceProtected:
                    return "protected-interface";
                case Glyph.InterfacePrivate:
                    return "private-interface";
                case Glyph.InterfaceInternal:
                    return "internal-interface";
                case Glyph.Label:
                    return "field";
                case Glyph.Local:
                    return "variable";
                case Glyph.Namespace:
                    return "name-space";
                case Glyph.MethodPublic:
                    return "method";
                case Glyph.MethodProtected:
                    return "protected-method";
                case Glyph.MethodPrivate:
                    return "private-method";
                case Glyph.MethodInternal:
                    return "internal-method";
                case Glyph.Parameter:
                    return "variable";
                case Glyph.PropertyPublic:
                    return "property";
                case Glyph.PropertyProtected:
                    return "property";
                case Glyph.PropertyPrivate:
                    return "property";
                case Glyph.PropertyInternal:
                    return "property";
                case Glyph.StructurePublic:
                    return "struct";
                case Glyph.StructureProtected:
                    return "protected-struct";
                case Glyph.StructurePrivate:
                    return "private-struct";
                case Glyph.StructureInternal:
                    return "internal-struct";
                case Glyph.TypeParameter:
                    return "field";
                default:
                    return null;
            }
        }
    }
}