// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal class CurlyBraceCompletionSession : AbstractTokenBraceCompletionSession
    {
        public CurlyBraceCompletionSession(ISyntaxFactsService syntaxFactsService)
            : base(syntaxFactsService, (int)SyntaxKind.OpenBraceToken, (int)SyntaxKind.CloseBraceToken,
                Braces.CurlyBrace.OpenCharacter, Braces.CurlyBrace.CloseCharacter)
        {
        }

        public override void AfterStart(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            FormatTrackingSpan(session, shouldHonorAutoFormattingOnCloseBraceOption: true);

            //session.TryMoveCaret(session.ClosingPoint - 1);
        }

        public override void AfterReturn(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            // check whether shape of the braces are what we support
            // shape must be either "{|}" or "{ }". | is where caret is. otherwise, we don't do any special behavior
            if (!ContainsOnlyWhitespace(session))
            {
                return;
            }

            // alright, it is in right shape.
            session.Document = session.Document.InsertText(session.ClosingPoint - 1, Environment.NewLine, cancellationToken);
            FormatTrackingSpan(session, shouldHonorAutoFormattingOnCloseBraceOption: false, rules: GetFormattingRules(session.Document));

            if (session.Document.TryGetText(out var text))
            {
                // put caret at right indentation
                PutCaretOnLine(session, text, text.Lines[text.Lines.GetLinePosition(session.OpeningPoint).Line + 1]);
            }
        }

        private static bool ContainsOnlyWhitespace(IBraceCompletionSession session)
        {
            var text = session.Text;
            var span = session.GetSessionSpan();

            var start = span.Start;
            start = text[start] == session.OpeningBrace ? start + 1 : start;

            var end = span.End - 1;
            end = text[end] == session.ClosingBrace ? end - 1 : end;

            if (start > text.Length ||
                end < 0 ||
                end > text.Length)
            {
                return false;
            }

            for (int i = start; i <= end; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerable<IFormattingRule> GetFormattingRules(Document document)
        {
            return SpecializedCollections.SingletonEnumerable(BraceCompletionFormattingRule.Instance).Concat(Formatter.GetDefaultFormattingRules(document));
        }

        private void FormatTrackingSpan(IBraceCompletionSession session, bool shouldHonorAutoFormattingOnCloseBraceOption, IEnumerable<IFormattingRule> rules = null)
        {
            var document = session.Document;
            var text = session.Text;

            var startPosition = session.OpeningPoint;
            var endPosition = session.ClosingPoint;

            // Do not format within the braces if they're on the same line for array/collection/object initializer expressions.
            // This is a heuristic to prevent brace completion from breaking user expectation/muscle memory in common scenarios.
            // see bug Devdiv:823958
            if (text.Lines.GetLinePosition(startPosition).Line == text.Lines.GetLinePosition(endPosition).Line)
            {
                // Brace completion is not cancellable
                var startToken = document.FindToken(startPosition, CancellationToken.None);
                if (startToken.IsKind(SyntaxKind.OpenBraceToken) &&
                    (startToken.Parent.IsInitializerForArrayOrCollectionCreationExpression() ||
                     startToken.Parent is AnonymousObjectCreationExpressionSyntax))
                {
                    // format everything but the brace pair.
                    var endToken = document.FindToken(endPosition, CancellationToken.None);
                    if (endToken.IsKind(SyntaxKind.CloseBraceToken))
                    {
                        endPosition = endPosition - (endToken.Span.Length + startToken.Span.Length);
                    }
                }
            }

            var style = document != null ? document.GetOptionsAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None).GetOption(FormattingOptions.SmartIndent)
                                         : FormattingOptions.SmartIndent.DefaultValue;

            if (style == FormattingOptions.IndentStyle.Smart)
            {
                // skip whitespace
                while (startPosition >= 0 && char.IsWhiteSpace(text[startPosition]))
                {
                    startPosition--;
                }

                // skip token
                startPosition--;
                while (startPosition >= 0 && !char.IsWhiteSpace(text[startPosition]))
                {
                    startPosition--;
                }
            }

            session.Document = session.Document.Format(TextSpan.FromBounds(Math.Max(startPosition, 0), endPosition), rules, CancellationToken.None);
        }

        private void PutCaretOnLine(IBraceCompletionSession session, SourceText text, TextLine line)
        {
            var indentation = GetDesiredIndentation(session, text);

            session.TryMoveCaret(line.Start + indentation);
        }

        private int GetDesiredIndentation(IBraceCompletionSession session, SourceText text)
        {
            // do poor man's indentation
            var openingPoint = session.OpeningPoint;
            var openingSpanLine = text.Lines.GetLineFromPosition(openingPoint);
            return openingPoint - openingSpanLine.Start;
        }

        private class BraceCompletionFormattingRule : BaseFormattingRule
        {
            private static readonly Predicate<SuppressOperation> _predicate = o => o == null || o.Option.IsOn(SuppressOption.NoWrapping);

            public static readonly IFormattingRule Instance = new BraceCompletionFormattingRule();

            public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
            {
                // Eg Cases -
                // new MyObject {
                // new List<int> {
                // int[] arr = {
                //           = new[] {
                //           = new int[] {
                if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent != null &&
                (currentToken.Parent.Kind() == SyntaxKind.ObjectInitializerExpression ||
                currentToken.Parent.Kind() == SyntaxKind.CollectionInitializerExpression ||
                currentToken.Parent.Kind() == SyntaxKind.ArrayInitializerExpression ||
                currentToken.Parent.Kind() == SyntaxKind.ImplicitArrayCreationExpression))
                {
                    if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers))
                    {
                        return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                    }
                    else
                    {
                        return null;
                    }
                }

                return base.GetAdjustNewLinesOperation(previousToken, currentToken, optionSet, nextOperation);
            }

            public override void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AlignTokensOperation> nextOperation)
            {
                base.AddAlignTokensOperations(list, node, optionSet, nextOperation);
                if (optionSet.GetOption(FormattingOptions.SmartIndent, node.Language) == FormattingOptions.IndentStyle.Block)
                {
                    var bracePair = node.GetBracePair();
                    if (bracePair.IsValidBracePair())
                    {
                        AddAlignIndentationOfTokensToBaseTokenOperation(list, node, bracePair.Item1, SpecializedCollections.SingletonEnumerable(bracePair.Item2), AlignTokensOption.AlignIndentationOfTokensToFirstTokenOfBaseTokenLine);
                    }
                }
            }

            public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<SuppressOperation> nextOperation)
            {
                base.AddSuppressOperations(list, node, optionSet, nextOperation);

                // remove suppression rules for array and collection initializer
                if (node.IsInitializerForArrayOrCollectionCreationExpression())
                {
                    // remove any suppression operation
                    list.RemoveAll(_predicate);
                }
            }
        }
    }
}
