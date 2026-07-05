// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.FileBasedPrograms;

public static partial class FileLevelDirectiveHelpers
{
    /// <summary>Finds file-level directives in the leading trivia list of a compilation unit and reports diagnostics on them.</summary>
    /// <param name="builder">The builder to store the parsed directives in, or null if the parsed directives are not needed.</param>
    public static ImmutableArray<FileLevelDirective> FindFileLevelDirectives(this SyntaxTree syntaxTree)
    {
        var builder = ImmutableArray.CreateBuilder<FileLevelDirective>();

        var triviaList = syntaxTree.GetRoot().GetLeadingTrivia();
        Debug.Assert(triviaList.Span.Start == 0);

        TextSpan previousWhiteSpaceSpan = default;

        for (var index = 0; index < triviaList.Count; index++)
        {
            var trivia = triviaList[index];
            // Stop when the trivia contains an error (e.g., because it's after #if).
            if (trivia.ContainsDiagnostics)
            {
                break;
            }

            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                Debug.Assert(previousWhiteSpaceSpan.IsEmpty);
                previousWhiteSpaceSpan = trivia.FullSpan;
                continue;
            }

            if (trivia.IsKind(SyntaxKind.ShebangDirectiveTrivia))
            {
                TextSpan span = GetFullSpan(previousWhiteSpaceSpan, trivia);

                var info = new FileLevelDirective(trivia, span, "shebang", trivia.ToString());

                builder.Add(info);
            }
            else if (trivia.IsKind(SyntaxKind.IgnoredDirectiveTrivia))
            {
                TextSpan span = GetFullSpan(previousWhiteSpaceSpan, trivia);

                var message = trivia.GetStructure() is IgnoredDirectiveTriviaSyntax { Content: { RawKind: (int)SyntaxKind.StringLiteralToken } content }
                    ? content.Text.AsSpan().Trim()
                    : "";
                var parts = Whitespace().Split(message.ToString(), 2);
                var name = parts.Length > 0 ? parts[0] : "";
                var value = parts.Length > 1 ? parts[1] : "";
                Debug.Assert(!(parts.Length > 2));

                var context = new FileLevelDirective(trivia, span, name, value);

                builder.Add(context);
            }

            previousWhiteSpaceSpan = default;
        }

        return builder.ToImmutable();

        static TextSpan GetFullSpan(TextSpan previousWhiteSpaceSpan, SyntaxTrivia trivia)
        {
            // Include the preceding whitespace in the span, i.e., span will be the whole line.
            return previousWhiteSpaceSpan.IsEmpty ? trivia.FullSpan : TextSpan.FromBounds(previousWhiteSpaceSpan.Start, trivia.FullSpan.End);
        }
    }


    [GeneratedRegex("""\s+""")]
    public static partial Regex Whitespace();
}

public readonly record struct FileLevelDirective(SyntaxTrivia Trivia, TextSpan Span, string DirectiveKind, string DirectiveText);
