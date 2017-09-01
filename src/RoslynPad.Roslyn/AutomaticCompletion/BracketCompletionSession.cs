// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.LanguageServices;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal class BracketCompletionSession : AbstractTokenBraceCompletionSession
    {
        public BracketCompletionSession(ISyntaxFactsService syntaxFactsService)
            : base(syntaxFactsService, (int)SyntaxKind.OpenBracketToken, (int)SyntaxKind.CloseBracketToken,
                  Braces.Bracket.OpenCharacter, Braces.Bracket.CloseCharacter)
        {
        }
    }
}
