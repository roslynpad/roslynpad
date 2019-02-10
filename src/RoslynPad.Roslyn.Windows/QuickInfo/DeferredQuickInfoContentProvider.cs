using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn.Completion;

namespace RoslynPad.Roslyn.QuickInfo
{
    [Export(typeof(IDeferredQuickInfoContentProvider))]
    internal class DeferredQuickInfoContentProvider : IDeferredQuickInfoContentProvider
    {
        public IDeferredQuickInfoContent CreateQuickInfoDisplayDeferredContent(
            ISymbol symbol,
            bool showWarningGlyph,
            bool showSymbolGlyph,
            IList<TaggedText> mainDescription,
            IDeferredQuickInfoContent documentation,
            IList<TaggedText> typeParameterMap,
            IList<TaggedText> anonymousTypes,
            IList<TaggedText> usageText,
            IList<TaggedText> exceptionText)
        {
            return new QuickInfoDisplayDeferredContent(
                symbolGlyph: showSymbolGlyph ? CreateGlyphDeferredContent(symbol) : null,
                warningGlyph: showWarningGlyph ? CreateWarningGlyph() : null,
                mainDescription: CreateClassifiableDeferredContent(mainDescription),
                documentation: documentation,
                typeParameterMap: CreateClassifiableDeferredContent(typeParameterMap),
                anonymousTypes: CreateClassifiableDeferredContent(anonymousTypes),
                usageText: CreateClassifiableDeferredContent(usageText),
                exceptionText: CreateClassifiableDeferredContent(exceptionText));
        }

        private static IDeferredQuickInfoContent CreateGlyphDeferredContent(ISymbol symbol)
        {
            return new SymbolGlyphDeferredContent(symbol.GetGlyph());
        }

        private static IDeferredQuickInfoContent CreateWarningGlyph()
        {
            return new SymbolGlyphDeferredContent(Glyph.CompletionWarning);
        }

        public IDeferredQuickInfoContent CreateDocumentationCommentDeferredContent(string? documentationComment)
        {
            return new DocumentationCommentDeferredContent(documentationComment);
        }

        public IDeferredQuickInfoContent CreateClassifiableDeferredContent(IList<TaggedText> content)
        {
            return new ClassifiableDeferredContent(content);
        }

        private class QuickInfoDisplayDeferredContent : IDeferredQuickInfoContent
        {
            private readonly IDeferredQuickInfoContent? _symbolGlyph;
            private readonly IDeferredQuickInfoContent? _warningGlyph;
            private readonly IDeferredQuickInfoContent _mainDescription;
            private readonly IDeferredQuickInfoContent _documentation;
            private readonly IDeferredQuickInfoContent _typeParameterMap;
            private readonly IDeferredQuickInfoContent _anonymousTypes;
            private readonly IDeferredQuickInfoContent _usageText;
            private readonly IDeferredQuickInfoContent _exceptionText;

            public QuickInfoDisplayDeferredContent(IDeferredQuickInfoContent? symbolGlyph, IDeferredQuickInfoContent? warningGlyph, IDeferredQuickInfoContent mainDescription, IDeferredQuickInfoContent documentation, IDeferredQuickInfoContent typeParameterMap, IDeferredQuickInfoContent anonymousTypes, IDeferredQuickInfoContent usageText, IDeferredQuickInfoContent exceptionText)
            {
                _symbolGlyph = symbolGlyph;
                _warningGlyph = warningGlyph;
                _mainDescription = mainDescription;
                _documentation = documentation;
                _typeParameterMap = typeParameterMap;
                _anonymousTypes = anonymousTypes;
                _usageText = usageText;
                _exceptionText = exceptionText;
            }

            public object Create()
            {
                object? warningGlyph = null;
                if (_warningGlyph != null)
                {
                    warningGlyph = _warningGlyph.Create();
                }

                object? symbolGlyph = null;
                if (_symbolGlyph != null)
                {
                    symbolGlyph = _symbolGlyph.Create();
                }

                return new QuickInfoDisplayPanel(
                    symbolGlyph as FrameworkElement,
                    warningGlyph as FrameworkElement, 
                    (FrameworkElement)_mainDescription.Create(),
                    (FrameworkElement)_documentation.Create(),
                    (FrameworkElement)_typeParameterMap.Create(),
                    (FrameworkElement)_anonymousTypes.Create(),
                    (FrameworkElement)_usageText.Create(),
                    (FrameworkElement)_exceptionText.Create());
            }
        }

        private class QuickInfoDisplayPanel : StackPanel
        {
            private TextBlock MainDescription { get; }
            private TextBlock Documentation { get; }
            private TextBlock TypeParameterMap { get; }
            private TextBlock AnonymousTypes { get; }
            private TextBlock UsageText { get; }
            private TextBlock ExceptionText { get; }

            public QuickInfoDisplayPanel(
                FrameworkElement? symbolGlyph,
                FrameworkElement? warningGlyph,
                FrameworkElement mainDescription,
                FrameworkElement documentation,
                FrameworkElement typeParameterMap,
                FrameworkElement anonymousTypes,
                FrameworkElement usageText,
                FrameworkElement exceptionText)
            {
                MainDescription = (TextBlock)mainDescription;
                Documentation = (TextBlock)documentation;
                TypeParameterMap = (TextBlock)typeParameterMap;
                AnonymousTypes = (TextBlock)anonymousTypes;
                UsageText = (TextBlock)usageText;
                ExceptionText = (TextBlock)exceptionText;

                Orientation = Orientation.Vertical;

                Border? symbolGlyphBorder = null;
                if (symbolGlyph != null)
                {
                    symbolGlyph.Margin = new Thickness(1, 1, 3, 1);
                    symbolGlyphBorder = new Border()
                    {
                        BorderThickness = new Thickness(0),
                        BorderBrush = Brushes.Transparent,
                        VerticalAlignment = VerticalAlignment.Top,
                        Child = symbolGlyph
                    };
                }

                mainDescription.Margin = new Thickness(1);
                var mainDescriptionBorder = new Border()
                {
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = mainDescription
                };

                var symbolGlyphAndMainDescriptionDock = new DockPanel()
                {
                    LastChildFill = true,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Background = Brushes.Transparent
                };

                if (symbolGlyphBorder != null)
                {
                    symbolGlyphAndMainDescriptionDock.Children.Add(symbolGlyphBorder);
                }

                symbolGlyphAndMainDescriptionDock.Children.Add(mainDescriptionBorder);

                if (warningGlyph != null)
                {
                    warningGlyph.Margin = new Thickness(1, 1, 3, 1);
                    var warningGlyphBorder = new Border()
                    {
                        BorderThickness = new Thickness(0),
                        BorderBrush = Brushes.Transparent,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Child = warningGlyph
                    };

                    symbolGlyphAndMainDescriptionDock.Children.Add(warningGlyphBorder);
                }

                Children.Add(symbolGlyphAndMainDescriptionDock);
                Children.Add(documentation);
                Children.Add(usageText);
                Children.Add(typeParameterMap);
                Children.Add(anonymousTypes);
                Children.Add(exceptionText);
            }

            public override string ToString()
            {
                var sb = new StringBuilder();

                BuildStringFromInlineCollection(MainDescription.Inlines, sb);

                if (Documentation.Inlines.Count > 0)
                {
                    sb.AppendLine();
                    BuildStringFromInlineCollection(Documentation.Inlines, sb);
                }

                if (TypeParameterMap.Inlines.Count > 0)
                {
                    sb.AppendLine();
                    BuildStringFromInlineCollection(TypeParameterMap.Inlines, sb);
                }

                if (AnonymousTypes.Inlines.Count > 0)
                {
                    sb.AppendLine();
                    BuildStringFromInlineCollection(AnonymousTypes.Inlines, sb);
                }

                if (UsageText.Inlines.Count > 0)
                {
                    sb.AppendLine();
                    BuildStringFromInlineCollection(UsageText.Inlines, sb);
                }

                if (ExceptionText.Inlines.Count > 0)
                {
                    sb.AppendLine();
                    BuildStringFromInlineCollection(ExceptionText.Inlines, sb);
                }

                return sb.ToString();
            }

            private static void BuildStringFromInlineCollection(InlineCollection inlines, StringBuilder sb)
            {
                foreach (var inline in inlines)
                {
                    if (inline != null)
                    {
                        var inlineText = GetStringFromInline(inline);
                        if (!string.IsNullOrEmpty(inlineText))
                        {
                            sb.Append(inlineText);
                        }
                    }
                }
            }

            private static string? GetStringFromInline(Inline currentInline)
            {
                if (currentInline is LineBreak lineBreak)
                {
                    return Environment.NewLine;
                }

                var run = currentInline as Run;
                return run?.Text;
            }
        }

        private class SymbolGlyphDeferredContent : IDeferredQuickInfoContent
        {
            public SymbolGlyphDeferredContent(Glyph glyph)
            {
                Glyph = glyph;
            }

            public object Create()
            {
                var image = new Image
                {
                    Width = 16,
                    Height = 16,
                    Source = Glyph.ToImageSource()
                };
                return image;
            }

            private Glyph Glyph { get; }
        }

        private class ClassifiableDeferredContent : IDeferredQuickInfoContent
        {
            private readonly IList<TaggedText> _classifiableContent;

            public ClassifiableDeferredContent(IList<TaggedText> content)
            {
                _classifiableContent = content;
            }

            public object Create()
            {
                var textBlock = _classifiableContent.ToTextBlock();
                if (textBlock.Inlines.Count == 0)
                    textBlock.Visibility = Visibility.Collapsed;
                return textBlock;
            }
        }

        private class DocumentationCommentDeferredContent : IDeferredQuickInfoContent
        {
            private readonly string? _documentationComment;

            public DocumentationCommentDeferredContent(string? documentationComment)
            {
                _documentationComment = documentationComment;
            }

            public object Create()
            {
                var documentationTextBlock = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap
                };

                UpdateDocumentationTextBlock(documentationTextBlock);
                return documentationTextBlock;
            }

            private void UpdateDocumentationTextBlock(TextBlock documentationTextBlock)
            {
                if (!string.IsNullOrEmpty(_documentationComment))
                {
                    documentationTextBlock.Text = _documentationComment;
                }
                else
                {
                    documentationTextBlock.Text = string.Empty;
                    documentationTextBlock.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}