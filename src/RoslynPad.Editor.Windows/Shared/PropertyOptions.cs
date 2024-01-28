using System;

namespace RoslynPad.Editor;

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
