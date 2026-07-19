#!/usr/bin/env python3
"""Generates Glyphs.axaml from the Visual Studio Image Library.

Usage: generate-glyphs.py <path-to-image-library-images-folder>

Each <name>.xaml (Viewbox > Canvas 16x16 > Path[Data, Fill, Opacity?]) becomes an Avalonia
DrawingGroup resource keyed by the image-catalog moniker name (see ImageCatalog.cs for the
id -> name map). A transparent 16x16 rectangle keeps the drawing bounds fixed regardless of
path extents.
"""

import os.path
import sys
import xml.etree.ElementTree as ET

if len(sys.argv) < 2:
    sys.exit("usage: generate-glyphs.py <images folder>")
LIBRARY = sys.argv[1]
OUTPUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Glyphs.axaml")

# The image-catalog monikers RoslynPad needs: every name Roslyn's Glyph mapping uses
# (vendor/roslyn LanguageServer/Protocol Extensions.KnownImageIds) plus the completion
# expander, the RoslynPad-specific AddReference glyph, and the light-bulb icons.
NAMES = """
Assembly VBFileNode VBProjectNode
ClassPublic ClassProtected ClassPrivate ClassInternal
CSFileNode CSProjectNode
ConstantPublic ConstantProtected ConstantPrivate ConstantInternal
DelegatePublic DelegateProtected DelegatePrivate DelegateInternal
EnumerationPublic EnumerationProtected EnumerationPrivate EnumerationInternal
EnumerationItemPublic
EventPublic EventProtected EventPrivate EventInternal
ExtensionMethod
FieldPublic FieldProtected FieldPrivate FieldInternal
InterfacePublic InterfaceProtected InterfacePrivate InterfaceInternal
IntellisenseKeyword IntellisenseWarning
Label LocalVariable MatchType
MethodPublic MethodProtected MethodPrivate MethodInternal
ModulePublic ModuleProtected ModulePrivate ModuleInternal
Namespace NuGet OpenFolder
OperatorPublic OperatorProtected OperatorPrivate OperatorInternal
PropertyPublic PropertyProtected PropertyPrivate PropertyInternal
Reference Snippet SparkleNoColor
StatusError StatusInformation StatusWarning
Type
ValueTypePublic ValueTypeProtected ValueTypePrivate ValueTypeInternal
ExpandScope AddReference
IntellisenseBulb IntellisenseLightBulbError Screwdriver
""".split()

WPF_NS = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"

def convert(name: str) -> str:
    tree = ET.parse(f"{LIBRARY}/{name}.xaml")
    canvas = tree.getroot().find(f"{WPF_NS}Canvas")
    assert canvas is not None, name
    assert canvas.get("Width") == "16" and canvas.get("Height") == "16", name

    lines = [f'  <DrawingGroup x:Key="{name}">']
    lines.append('    <DrawingGroup.Children>')
    lines.append('      <GeometryDrawing Brush="#00FFFFFF" Geometry="F1M16,16L0,16 0,0 16,0z" />')
    for path in canvas:
        assert path.tag == f"{WPF_NS}Path", (name, path.tag)
        data = path.get("Data")
        fill = path.get("Fill")
        opacity = path.get("Opacity")
        assert data and fill, name
        extra = set(path.keys()) - {"Data", "Fill", "Opacity"}
        assert not extra, (name, extra)
        drawing = f'<GeometryDrawing Brush="{fill}" Geometry="{data}" />'
        if opacity:
            lines.append(f'      <DrawingGroup Opacity="{opacity}">')
            lines.append('        <DrawingGroup.Children>')
            lines.append(f'          {drawing}')
            lines.append('        </DrawingGroup.Children>')
            lines.append('      </DrawingGroup>')
        else:
            lines.append(f'      {drawing}')
    lines.append('    </DrawingGroup.Children>')
    lines.append('  </DrawingGroup>')
    return "\n".join(lines)

header = '''<!-- Generated from the Visual Studio 2026 Image Library; keys are image-catalog
     moniker names (see ImageCatalog.cs). Regenerate rather than editing by hand. -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    x:Class="Morgania.CodeAnalysis.Editor.Resources.Glyphs">
'''

with open(OUTPUT, "w") as f:
    f.write(header)
    for name in NAMES:
        f.write(convert(name))
        f.write("\n")
    f.write("</ResourceDictionary>\n")

print(f"wrote {len(NAMES)} glyphs to {OUTPUT}")
