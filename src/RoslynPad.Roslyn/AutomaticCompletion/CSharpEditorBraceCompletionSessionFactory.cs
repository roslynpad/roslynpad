using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    [ExportLanguageService(typeof(IEditorBraceCompletionSessionFactory), LanguageNames.CSharp), Shared]
    internal class CSharpEditorBraceCompletionSessionFactory : AbstractEditorBraceCompletionSessionFactory
    {
        protected override bool IsSupportedOpeningBrace(char openingBrace)
        {
            switch (openingBrace)
            {
                case Braces.Bracket.OpenCharacter:
                case Braces.CurlyBrace.OpenCharacter:
                case Braces.Parenthesis.OpenCharacter:
                case Braces.SingleQuote.OpenCharacter:
                case Braces.DoubleQuote.OpenCharacter:
                case Braces.LessAndGreaterThan.OpenCharacter:
                    return true;
            }

            return false;
        }

        protected override bool CheckCodeContext(Document document, int position, char openingBrace, CancellationToken cancellationToken)
        {
            // SPECIAL CASE: Allow in curly braces in string literals to support interpolated strings.
            if (openingBrace == Braces.CurlyBrace.OpenCharacter &&
                InterpolationCompletionSession.IsContext(document, position, cancellationToken))
            {
                return true;
            }

            if (openingBrace == Braces.DoubleQuote.OpenCharacter &&
                InterpolatedStringCompletionSession.IsContext(document, position, cancellationToken))
            {
                return true;
            }

            // Otherwise, defer to the base implementation.
            return base.CheckCodeContext(document, position, openingBrace, cancellationToken);
        }

        protected override IEditorBraceCompletionSession CreateEditorSession(Document document, int openingPosition, char openingBrace, CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            switch (openingBrace)
            {
                case Braces.CurlyBrace.OpenCharacter:
                    return InterpolationCompletionSession.IsContext(document, openingPosition, cancellationToken)
                        ? new InterpolationCompletionSession()
                        : (IEditorBraceCompletionSession)new CurlyBraceCompletionSession(syntaxFactsService);

                case Braces.DoubleQuote.OpenCharacter:
                    return InterpolatedStringCompletionSession.IsContext(document, openingPosition, cancellationToken)
                        ? new InterpolatedStringCompletionSession()
                        : (IEditorBraceCompletionSession)new StringLiteralCompletionSession(syntaxFactsService);

                case Braces.Bracket.OpenCharacter: return new BracketCompletionSession(syntaxFactsService);
                case Braces.Parenthesis.OpenCharacter: return new ParenthesisCompletionSession(syntaxFactsService);
                case Braces.SingleQuote.OpenCharacter: return new CharLiteralCompletionSession(syntaxFactsService);
                case Braces.LessAndGreaterThan.OpenCharacter: return new LessAndGreaterThanCompletionSession(syntaxFactsService);
                default:
                    throw new InvalidOperationException("Unexpected value: " + openingBrace);
            }
        }
    }
}