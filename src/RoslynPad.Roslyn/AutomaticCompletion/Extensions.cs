// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Formatting;
using System.Linq;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal static class Extensions
    {
        public static Document Format(this Document document, TextSpan span, IEnumerable<IFormattingRule> rules, CancellationToken cancellationToken)
        {
            rules = GetFormattingRules(document, rules, span);
            var syntaxRoot = document.GetSyntaxRootSynchronously(cancellationToken);
            var options = document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var formattedTextChanges = Formatter.GetFormattedTextChanges(syntaxRoot,
                SpecializedCollections.SingletonEnumerable(span), document.Project.Solution.Workspace,
                options, rules, cancellationToken);
            return document.Project.Solution.Workspace.ApplyTextChanges(document, formattedTextChanges, cancellationToken);
        }

        internal static Document ApplyTextChanges(this Workspace workspace, Document document, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken)
        {
            var newSolution = workspace.CurrentSolution.UpdateDocument(document.Id, textChanges, cancellationToken);
            if (workspace.TryApplyChanges(newSolution))
            {
                return newSolution.Workspace.CurrentSolution.GetDocument(document.Id);
            }

            return document;
        }

        private static IEnumerable<IFormattingRule> GetFormattingRules(Document document, IEnumerable<IFormattingRule> rules, TextSpan span)
        {
            var ruleFactoryService = document.Project.Solution.Workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
            int position = (span.Start + span.End) / 2;
            return SpecializedCollections.SingletonEnumerable(ruleFactoryService.CreateRule(document, position))
                .Concat(rules ?? Formatter.GetDefaultFormattingRules(document));
        }


        ///// <summary>
        ///// create caret preserving edit transaction with automatic code change undo merging policy
        ///// </summary>
        //public static CaretPreservingEditTransaction CreateEditTransaction(
        //    this ITextView view, string description, ITextUndoHistoryRegistry registry, IEditorOperationsFactoryService service)
        //{
        //    return new CaretPreservingEditTransaction(description, view, registry, service)
        //    {
        //        MergePolicy = AutomaticCodeChangeMergePolicy.Instance
        //    };
        //}

        public static SyntaxToken FindToken(this Document document, int position, CancellationToken cancellationToken)
        {
            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            return root.FindToken(position, findInsideTrivia: true);
        }

        /// <summary>
        /// insert text to workspace and get updated version of the document
        /// </summary>
        public static Document InsertText(this Document document, int position, string text, CancellationToken cancellationToken = default)
        {
            return document.ReplaceText(new TextSpan(position, 0), text, cancellationToken);
        }

        /// <summary>
        /// replace text to workspace and get updated version of the document
        /// </summary>
        public static Document ReplaceText(this Document document, TextSpan span, string text, CancellationToken cancellationToken)
        {
            return document.ApplyTextChange(new TextChange(span, text), cancellationToken);
        }

        /// <summary>
        /// apply text changes to workspace and get updated version of the document
        /// </summary>
        public static Document ApplyTextChange(this Document document, TextChange textChange, CancellationToken cancellationToken)
        {
            return document.ApplyTextChanges(SpecializedCollections.SingletonEnumerable(textChange), cancellationToken);
        }

        /// <summary>
        /// apply text changes to workspace and get updated version of the document
        /// </summary>
        public static Document ApplyTextChanges(this Document document, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken)
        {
            // here assumption is that text change are based on current solution
            var oldSolution = document.Project.Solution;
            var newSolution = oldSolution.UpdateDocument(document.Id, textChanges, cancellationToken);

            if (oldSolution.Workspace.TryApplyChanges(newSolution))
            {
                return newSolution.Workspace.CurrentSolution.GetDocument(document.Id);
            }

            return document;
        }

        /// <summary>
        /// Update the solution so that the document with the Id has the text changes
        /// </summary>
        public static Solution UpdateDocument(this Solution solution, DocumentId id, IEnumerable<TextChange> textChanges, CancellationToken cancellationToken = default)
        {
            var oldDocument = solution.GetDocument(id);
            var newText = oldDocument.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken).WithChanges(textChanges);
            return solution.WithDocumentText(id, newText);
        }

        public static TextSpan GetSessionSpan(this IBraceCompletionSession session)
        {
            var open = session.OpeningPoint;
            var close = session.ClosingPoint;

            return TextSpan.FromBounds(open, close);
        }

        public static int GetValueInValidRange(this int value, int smallest, int largest)
        {
            return Math.Max(smallest, Math.Min(value, largest));
        }
    }
}
