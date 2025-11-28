// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace RoslynPad.Editor;

internal sealed class SnippetManager
{
    internal readonly ImmutableDictionary<string, CodeSnippet> _defaultSnippets;

    public SnippetManager()
    {
        var snippets = GetGeneralSnippets();
        snippets.AddRange(GetPlatformSnippets());

        _defaultSnippets = snippets.ToImmutableDictionary(x => x.Name);
    }

    public IEnumerable<CodeSnippet> Snippets => _defaultSnippets.Values;
    
    public CodeSnippet? FindSnippet(string name)
    {
        _defaultSnippets.TryGetValue(name, out var snippet);
        return snippet;
    }

    private List<CodeSnippet> GetGeneralSnippets()
    {
        var snippets = new List<CodeSnippet>
        {
            new            (
                "for",
                "for loop",
                "for (int ${counter=i} = 0; ${counter} < ${end}; ${counter}++)\n{\n\t${Selection}\n}",
                "for"
            ),
            new            (
                "forr",
                "reverse for loop",
                "for (int ${counter=i} = ${length} - 1; ${counter} >= 0; ${counter}--)\n{\n\t${Selection}\n}",
                "for"
            ),
            new            (
                "foreach",
                "foreach loop",
                "foreach (${var} ${element} in ${collection})\n{\n\t${Selection}\n}",
                "foreach"
            ),
            new            (
                "if",
                "if statement",
                "if (${condition})\n{\n\t${Selection}\n}",
                "if"
            ),
            new            (
                "ifnull",
                "if-null statement",
                "if (${condition} == null)\n{\n\t${Selection}\n}",
                "if"
            ),
            new            (
                "ifnotnull",
                "if-not-null statement",
                "if (${condition} != null)\n{\n\t${Selection}\n}",
                "if"
            ),
            new            (
                "ifelse",
                "if-else statement",
                "if (${condition})\n{\n\t${Selection}\n}\nelse\n{\n\t${Caret}\n}",
                "if"
            ),
            new            (
                "else",
                "Code snippet for else statement",
                "else\n{\n\t${Selection} ${Caret}\n}",
                "if"
            ),
            new            (
                "while",
                "while loop",
                "while (${condition})\n{\n\t${Selection}\n}",
                "while"
            ),
            new            (
                "do",
                "Code snippet for do...while loop",
                "do\n{\n\t${Selection} ${Caret}\n} while (${expression=true});",
                "for"
            ),
            new            (
                "prop",
                "Property",
                "public ${Type=object} ${Property=Property} { get; set; }${Caret}",
                "class" // properties can be declared in class/struct/interface
            ),
            new            (
                "propg",
                "Property with private setter",
                "public ${Type=object} ${Property=Property} { get; private set; }${Caret}",
                "class"
            ),
            new            (
                "propfull",
                "Property with backing field",
                "${type} ${toFieldName(name)};\n\npublic ${type=int} ${name=Property}\n{\n\tget { return ${toFieldName(name)}; }\n\tset { ${toFieldName(name)} = value; }\n}${Caret}",
                "class"
            ),
            new            (
                "propdp",
                "Dependency Property",
                "public static readonly DependencyProperty ${name}Property =" + Environment.NewLine
                       + "\tDependencyProperty.Register(\"${name}\", typeof(${type}), typeof(${ClassName})," +
                       Environment.NewLine
                       + "\t                            new FrameworkPropertyMetadata());" + Environment.NewLine
                       + "" + Environment.NewLine
                       + "public ${type=int} ${name=Property}\n{" + Environment.NewLine
                       + "\tget { return (${type})GetValue(${name}Property); }" + Environment.NewLine
                       + "\tset { SetValue(${name}Property, value); }"
                       + Environment.NewLine + "}${Caret}",
                "class"
            ),
            new            (
                "switch",
                "Switch statement",
                "switch (${condition})\n{\n\t${Caret}\n}",
                "switch"
            ),
            new            (
                "try",
                "Try-catch statement",
                "try\n{\n\t${Selection}\n}\ncatch (Exception)\n{\n\t${Caret}\n\tthrow;\n}",
                "try"
            ),
            new            (
                "trycf",
                "Try-catch-finally statement",
                "try\n{\n\t${Selection}\n}\ncatch (Exception)\n{\n\t${Caret}\n\tthrow;\n}\nfinally\n{\n\t\n}",
                "try"
            ),
            new            (
                "tryf",
                "Try-finally statement",
                "try\n{\n\t${Selection}\n}\nfinally\n{\n\t${Caret}\n}",
                "try"
            ),
            new            (
                "using",
                "Using statement",
                "using (${resource=null})\n{\n\t${Selection}\n}",
                "try"
            ),
            new            (
                "cw",
                "Console.WriteLine",
                "Console.WriteLine(${Selection})",
                "if"
            ),
            new            (
                "namespace",
                "Namespace declaration",
                "namespace ${Namespace}\n{\n\t${Caret}\n}",
                "namespace"
            ),
            new            (
                "class",
                "Class declaration",
                "public class ${ClassName}\n{\n\t${Caret}\n}",
                "class"
            ),
            new            (
                "interface",
                "Interface declaration",
                "public interface ${InterfaceName}\n{\n\t${Caret}\n}",
                "interface"
            ),
            new            (
                "struct",
                "Struct declaration",
                "public struct ${StructName}\n{\n\t${Caret}\n}",
                "struct"
            ),
            new            (
                "enum",
                "Enum declaration",
                "public enum ${EnumName}\n{\n\t${Caret}\n}",
                "enum"
            ),
            new            (
                "method",
                "Method declaration",
                "public ${ReturnType=void} ${MethodName}()\n{\n\t${Caret}\n}",
                "method"
            ),
            new             (
                "sim",
                "Code snippet for int Main()",
                "static int Main(string[] args)\n{\n\t${Caret}\n\treturn 0;\n}",
                "class"
            ),
            new             (
                "svm",
                "Code snippet for 'void Main' method",
                "static void Main(string[] args)\n{\n\t${Caret}\n}",
                "class"
            ),
            new             (
                "unchecked",
                "Code snippet for unchecked block",
                "unchecked\n{\n\t${Selection} ${Caret}\n}",
                "checked"
            ),
            new             (
                "checked",
                "Code snippet for checked block",
                "checked\n{\n\t${Selection} ${Caret}\n}",
                "checked"
            ),
            new             (
                "unsafe",
                "Code snippet for unsafe statement",
                "unsafe\n{\n\t${Selection} ${Caret}\n}",
                "class"
            ),
            new             (
                "lock",
                "Code snippet for lock statement",
                "lock (${expression=this})\n{\n\t${Selection} ${Caret}\n}",
                "class"
            ),
            new             (
                "ctor",
                "Code snippet for constructor",
                "public ${ClassName}()\n{\n\t${Caret}\n}",
                "class"
            ),
            new            (
                "~",
                "Code snippet for destructor",
                "~${ClassName}()\n{\n\t${Caret}\n}",
                "class"
            ),
            new             (
                "#if",
                "Code snippet for #if",
                "#if ${expression=true}\n\t${Selection} ${Caret} \n#endif",
                "class"
            ),
            new             (
                "#region",
                "Code snippet for #region",
                "#region ${name=MyRegion}\n\t${Selection} ${Caret}\n#endregion",
                "class"
            ),
            new             (
                "indexer",
                "Code snippet for indexer",
                "${access=public} ${type=object} this[${indextype=int} index]\n{\n\tget {${Caret} /* return the specified index here */ }\n\tset { /* set the specified index to value here */ }\n}",
                "class"
            ),
        };
        return snippets;
    }

    private List<CodeSnippet> GetPlatformSnippets()
    {
        var snippets = new List<CodeSnippet>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            snippets.AddRange(GetWindowsSnippets());
        }
        return snippets;
    }

    private List<CodeSnippet> GetWindowsSnippets()
    {
        return
        [
            new CodeSnippet
            (
                "desktopapp",
                "#r Framework-include and await Helpers.RunWpfAsync()",
                "#r \"framework: Microsoft.WindowsDesktop.App\"\nawait Helpers.RunWpfAsync();\n\n${Selection}",
                "#r"
            )
        ];
    }
}
