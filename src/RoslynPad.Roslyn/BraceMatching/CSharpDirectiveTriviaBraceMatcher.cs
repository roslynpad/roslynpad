﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.BraceMatching;

[ExportBraceMatcher(LanguageNames.CSharp)]
internal class CSharpDirectiveTriviaBraceMatcher : AbstractDirectiveTriviaBraceMatcher<DirectiveTriviaSyntax, IfDirectiveTriviaSyntax, ElifDirectiveTriviaSyntax, ElseDirectiveTriviaSyntax, EndIfDirectiveTriviaSyntax>
{
    internal override List<DirectiveTriviaSyntax> GetMatchingConditionalDirectives(DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
            => [.. directive.GetMatchingConditionalDirectives(cancellationToken)];

    internal override DirectiveTriviaSyntax? GetMatchingDirective(DirectiveTriviaSyntax directive, CancellationToken cancellationToken)
            => directive.GetMatchingDirective(cancellationToken);

    internal override TextSpan GetSpanForTagging(DirectiveTriviaSyntax directive)
            => TextSpan.FromBounds(directive.HashToken.SpanStart, directive.DirectiveNameToken.Span.End);
}
