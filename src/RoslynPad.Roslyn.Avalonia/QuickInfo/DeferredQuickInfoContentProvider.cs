using System.Collections.Generic;
using System.Composition;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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
                    symbolGlyph as Control,
                    warningGlyph as Control, 
                    (Control)_mainDescription.Create(),
                    (Control)_documentation.Create(),
                    (Control)_typeParameterMap.Create(),
                    (Control)_anonymousTypes.Create(),
                    (Control)_usageText.Create(),
                    (Control)_exceptionText.Create());
            }
        }

        private class QuickInfoDisplayPanel : StackPanel
        {
            public QuickInfoDisplayPanel(
                Control? symbolGlyph,
                Control? warningGlyph,
                Control mainDescription,
                Control documentation,
                Control typeParameterMap,
                Control anonymousTypes,
                Control usageText,
                Control exceptionText)
            {
                Orientation = Orientation.Vertical;

                Border? symbolGlyphBorder = null;
                if (symbolGlyph != null)
                {
                    symbolGlyph.Margin = new Thickness(1, 1, 3, 1);
                    symbolGlyphBorder = new Border()
                    {
                        BorderThickness = new Thickness(),
                        BorderBrush = Brushes.Transparent,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                        Child = symbolGlyph
                    };
                }

                mainDescription.Margin = new Thickness(1);
                var mainDescriptionBorder = new Border()
                {
                    BorderThickness = new Thickness(),
                    BorderBrush = Brushes.Transparent,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Child = mainDescription
                };

                var symbolGlyphAndMainDescriptionDock = new DockPanel()
                {
                    LastChildFill = true,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
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
                        BorderThickness = new Thickness(),
                        BorderBrush = Brushes.Transparent,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
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
        }

        private class SymbolGlyphDeferredContent : IDeferredQuickInfoContent
        {
            public SymbolGlyphDeferredContent(Glyph glyph)
            {
                Glyph = glyph;
            }

            public object Create()
            {
                var image = new DrawingPresenter
                {
                    Width = 16,
                    Height = 16,
                    Drawing = Glyph.ToImageSource()
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

            public object Create() => _classifiableContent.ToTextBlock();
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
                    documentationTextBlock.IsVisible = false;
                }
            }
        }
    }
}