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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace RoslynPad.Editor
{
    internal sealed class SnippetManager
    {
        internal readonly ImmutableDictionary<string, CodeSnippet> DefaultSnippets;

        public SnippetManager()
        {
            var snippets = GetGeneralSnippets();
            snippets.AddRange(GetPlatformSnippets());

            DefaultSnippets = snippets.ToImmutableDictionary(x => x.Name);
        }

        public IEnumerable<CodeSnippet> Snippets => DefaultSnippets.Values;
        
        public CodeSnippet? FindSnippet(string name)
        {
            DefaultSnippets.TryGetValue(name, out var snippet);
            return snippet;
        }

        private List<CodeSnippet> GetGeneralSnippets()
        {
            var snippets = new List<CodeSnippet>
            {
                new CodeSnippet
                (
                    "for",
                    "for loop",
                    "for (int ${counter=i} = 0; ${counter} < ${end}; ${counter}++)\n{\n\t${Selection}\n}",
                    "for"
                ),
                new CodeSnippet
                (
                    "foreach",
                    "foreach loop",
                    "foreach (${var} ${element} in ${collection})\n{\n\t${Selection}\n}",
                    "foreach"
                ),
                new CodeSnippet
                (
                    "if",
                    "if statement",
                    "if (${condition})\n{\n\t${Selection}\n}",
                    "if"
                ),
                new CodeSnippet
                (
                    "ifnull",
                    "if-null statement",
                    "if (${condition} == null)\n{\n\t${Selection}\n}",
                    "if"
                ),
                new CodeSnippet
                (
                    "ifnotnull",
                    "if-not-null statement",
                    "if (${condition} != null)\n{\n\t${Selection}\n}",
                    "if"
                ),
                new CodeSnippet
                (
                    "ifelse",
                    "if-else statement",
                    "if (${condition})\n{\n\t${Selection}\n}\nelse\n{\n\t${Caret}\n}",
                    "if"
                ),
                new CodeSnippet
                (
                    "while",
                    "while loop",
                    "while (${condition})\n{\n\t${Selection}\n}",
                    "while"
                ),
                new CodeSnippet
                (
                    "prop",
                    "Property",
                    "public ${Type=object} ${Property=Property} { get; set; }${Caret}",
                    "event" // properties can be declared where events can be.
                ),
                new CodeSnippet
                (
                    "propg",
                    "Property with private setter",
                    "public ${Type=object} ${Property=Property} { get; private set; }${Caret}",
                    "event"
                ),
                new CodeSnippet
                (
                    "propfull",
                    "Property with backing field",
                    "${type} ${toFieldName(name)};\n\npublic ${type=int} ${name=Property}\n{\n\tget { return ${toFieldName(name)}; }\n\tset { ${toFieldName(name)} = value; }\n}${Caret}",
                    "event"
                ),
                new CodeSnippet
                (
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
                    "event"
                ),
                new CodeSnippet
                (
                    "switch",
                    "Switch statement",
                    "switch (${condition})\n{\n\t${Caret}\n}",
                    "switch"
                ),
                new CodeSnippet
                (
                    "try",
                    "Try-catch statement",
                    "try\n{\n\t${Selection}\n}\ncatch (Exception)\n{\n\t${Caret}\n\tthrow;\n}",
                    "try"
                ),
                new CodeSnippet
                (
                    "trycf",
                    "Try-catch-finally statement",
                    "try\n{\n\t${Selection}\n}\ncatch (Exception)\n{\n\t${Caret}\n\tthrow;\n}\nfinally\n{\n\t\n}",
                    "try"
                ),
                new CodeSnippet
                (
                    "tryf",
                    "Try-finally statement",
                    "try\n{\n\t${Selection}\n}\nfinally\n{\n\t${Caret}\n}",
                    "try"
                ),
                new CodeSnippet
                (
                    "using",
                    "Using statement",
                    "using (${resource=null})\n{\n\t${Selection}\n}",
                    "try"
                ),
                new CodeSnippet
                (
                    "cw",
                    "Console.WriteLine",
                    "Console.WriteLine(${Selection})",
                    "if"
                )
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
            return new List<CodeSnippet>
            {
                new CodeSnippet
                (
                    "desktopapp",
                    "#r Framework-include and await Helpers.RunWpfAsync()",
                    "#r \"framework: Microsoft.WindowsDesktop.App\"\nawait Helpers.RunWpfAsync();\n\n${Selection}",
                    "#r"
                )
            };
        }
    }
}
