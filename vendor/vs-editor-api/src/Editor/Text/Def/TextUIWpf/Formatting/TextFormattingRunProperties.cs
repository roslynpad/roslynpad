//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation of the WPF-only formatting-properties class
//  (PLAN §3.3/§5.4: recreated from public documentation, learn.microsoft.com
//  "Microsoft.VisualStudio.Text.Formatting.TextFormattingRunProperties"; the
//  proprietary Text.UI.Wpf binary is never referenced). Signature-adapted per
//  PLAN §4.2: WPF TextRunProperties/Brush/Typeface become their Avalonia
//  equivalents; the WPF-only TextEffects member is omitted; serialization
//  support (a WPF settings-store concern) is omitted.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    using System;
    using System.Globalization;
    using Avalonia.Media;
    using Avalonia.Media.TextFormatting;

    /// <summary>
    /// Immutable formatting properties for a run of text. Instances are created via the
    /// <c>CreateTextFormattingRunProperties</c> factory methods and modified with the
    /// <c>Set*</c>/<c>Clear*</c> methods, each of which returns a new instance.
    /// A property can be "empty", meaning this run does not contribute a value for it
    /// (the corresponding <c>*Empty</c> property is true); merging done by the
    /// classification format map relies on this tri-state.
    /// </summary>
    public sealed class TextFormattingRunProperties : TextRunProperties, IEquatable<TextFormattingRunProperties>
    {
        private static readonly Typeface DefaultTypeface = new Typeface("Consolas, Menlo, DejaVu Sans Mono, Cascadia Mono, Courier New, monospace");
        private const double DefaultRenderingSize = 13.0;

        private readonly Typeface? _typeface;
        private readonly double? _fontRenderingEmSize;
        private readonly double? _fontHintingEmSize;
        private readonly IBrush _foregroundBrush;
        private readonly IBrush _backgroundBrush;
        private readonly bool? _bold;
        private readonly bool? _italic;
        private readonly double? _foregroundOpacity;
        private readonly double? _backgroundOpacity;
        private readonly TextDecorationCollection _textDecorations;
        private readonly CultureInfo _cultureInfo;

        private TextFormattingRunProperties(
            Typeface? typeface, double? fontRenderingEmSize, double? fontHintingEmSize,
            IBrush foregroundBrush, IBrush backgroundBrush,
            bool? bold, bool? italic,
            double? foregroundOpacity, double? backgroundOpacity,
            TextDecorationCollection textDecorations, CultureInfo cultureInfo)
        {
            _typeface = typeface;
            _fontRenderingEmSize = fontRenderingEmSize;
            _fontHintingEmSize = fontHintingEmSize;
            _foregroundBrush = foregroundBrush;
            _backgroundBrush = backgroundBrush;
            _bold = bold;
            _italic = italic;
            _foregroundOpacity = foregroundOpacity;
            _backgroundOpacity = backgroundOpacity;
            _textDecorations = textDecorations;
            _cultureInfo = cultureInfo;
        }

        /// <summary>Creates a set of properties in which every property is empty.</summary>
        public static TextFormattingRunProperties CreateTextFormattingRunProperties()
        {
            return new TextFormattingRunProperties(null, null, null, null, null, null, null, null, null, null, null);
        }

        /// <summary>Creates a set of properties with the given typeface, size and foreground color.</summary>
        public static TextFormattingRunProperties CreateTextFormattingRunProperties(Typeface typeface, double size, Color foreground)
        {
            return new TextFormattingRunProperties(typeface, size, null, new SolidColorBrush(foreground), null, null, null, null, null, null, null);
        }

        /// <summary>
        /// Creates a fully specified set of properties. Any argument may be null, leaving
        /// the corresponding property empty. (The WPF overload's TextEffectCollection
        /// parameter is omitted in the retyped tier.)
        /// </summary>
        public static TextFormattingRunProperties CreateTextFormattingRunProperties(
            IBrush foreground, IBrush background, Typeface? typeface,
            double? size, double? hintingSize,
            TextDecorationCollection textDecorations, CultureInfo cultureInfo)
        {
            return new TextFormattingRunProperties(typeface, size, hintingSize, foreground, background, null, null, null, null, textDecorations, cultureInfo);
        }

        #region TextRunProperties (Avalonia) overrides

        /// <summary>The effective typeface, with any explicit bold/italic settings applied.</summary>
        public override Typeface Typeface
        {
            get
            {
                Typeface typeface = _typeface ?? DefaultTypeface;
                if (_bold.HasValue || _italic.HasValue)
                {
                    FontWeight weight = _bold.HasValue ? (_bold.Value ? FontWeight.Bold : FontWeight.Normal) : typeface.Weight;
                    FontStyle style = _italic.HasValue ? (_italic.Value ? FontStyle.Italic : FontStyle.Normal) : typeface.Style;
                    typeface = new Typeface(typeface.FontFamily, style, weight, typeface.Stretch);
                }

                return typeface;
            }
        }

        /// <summary>The effective font rendering size, in device-independent pixels.</summary>
        public override double FontRenderingEmSize => _fontRenderingEmSize ?? DefaultRenderingSize;

        /// <summary>The effective foreground brush, with any foreground opacity applied.</summary>
        public override IBrush ForegroundBrush
        {
            get
            {
                IBrush brush = _foregroundBrush ?? Brushes.Black;
                return ApplyOpacity(brush, _foregroundOpacity);
            }
        }

        /// <summary>The effective background brush (null when empty), with any background opacity applied.</summary>
        public override IBrush BackgroundBrush => _backgroundBrush == null ? null : ApplyOpacity(_backgroundBrush, _backgroundOpacity);

        /// <summary>The text decorations, or null when empty.</summary>
        public override TextDecorationCollection TextDecorations => _textDecorations;

        /// <summary>The culture used for formatting, or null when empty.</summary>
        public override CultureInfo CultureInfo => _cultureInfo;

        /// <summary>Baseline alignment; always <see cref="BaselineAlignment.Baseline"/> for editor text.</summary>
        public override BaselineAlignment BaselineAlignment => BaselineAlignment.Baseline;

        #endregion

        #region VS surface

        /// <summary>Determines whether the text is bold. Meaningful only when <see cref="BoldEmpty"/> is false.</summary>
        public bool Bold => _bold ?? (_typeface.HasValue && _typeface.Value.Weight >= FontWeight.Bold);

        /// <summary>Determines whether the text is italic. Meaningful only when <see cref="ItalicEmpty"/> is false.</summary>
        public bool Italic => _italic ?? (_typeface.HasValue && _typeface.Value.Style == FontStyle.Italic);

        /// <summary>The font hinting size. Meaningful only when <see cref="FontHintingEmSizeEmpty"/> is false.</summary>
        public double FontHintingEmSize => _fontHintingEmSize ?? 0.0;

        /// <summary>The foreground opacity. Meaningful only when <see cref="ForegroundOpacityEmpty"/> is false.</summary>
        public double ForegroundOpacity => _foregroundOpacity ?? 1.0;

        /// <summary>The background opacity. Meaningful only when <see cref="BackgroundOpacityEmpty"/> is false.</summary>
        public double BackgroundOpacity => _backgroundOpacity ?? 1.0;

        /// <summary>Determines whether the bold property is unset for this run.</summary>
        public bool BoldEmpty => !_bold.HasValue;

        /// <summary>Determines whether the italic property is unset for this run.</summary>
        public bool ItalicEmpty => !_italic.HasValue;

        /// <summary>Determines whether the typeface is unset for this run.</summary>
        public bool TypefaceEmpty => !_typeface.HasValue;

        /// <summary>Determines whether the foreground brush is unset for this run.</summary>
        public bool ForegroundBrushEmpty => _foregroundBrush == null;

        /// <summary>Determines whether the background brush is unset for this run.</summary>
        public bool BackgroundBrushEmpty => _backgroundBrush == null;

        /// <summary>Determines whether the font rendering size is unset for this run.</summary>
        public bool FontRenderingEmSizeEmpty => !_fontRenderingEmSize.HasValue;

        /// <summary>Determines whether the font hinting size is unset for this run.</summary>
        public bool FontHintingEmSizeEmpty => !_fontHintingEmSize.HasValue;

        /// <summary>Determines whether the text decorations are unset for this run.</summary>
        public bool TextDecorationsEmpty => _textDecorations == null;

        /// <summary>Determines whether the culture is unset for this run.</summary>
        public bool CultureInfoEmpty => _cultureInfo == null;

        /// <summary>Determines whether the foreground opacity is unset for this run.</summary>
        public bool ForegroundOpacityEmpty => !_foregroundOpacity.HasValue;

        /// <summary>Determines whether the background opacity is unset for this run.</summary>
        public bool BackgroundOpacityEmpty => !_backgroundOpacity.HasValue;

        /// <summary>Returns new properties with bold set.</summary>
        public TextFormattingRunProperties SetBold(bool isBold)
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, isBold, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with italic set.</summary>
        public TextFormattingRunProperties SetItalic(bool isItalic)
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, _bold, isItalic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the foreground set to a solid color brush.</summary>
        public TextFormattingRunProperties SetForeground(Color foreground)
            => SetForegroundBrush(new SolidColorBrush(foreground));

        /// <summary>Returns new properties with the given foreground brush.</summary>
        public TextFormattingRunProperties SetForegroundBrush(IBrush brush)
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, brush, _backgroundBrush, _bold, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the background set to a solid color brush.</summary>
        public TextFormattingRunProperties SetBackground(Color background)
            => SetBackgroundBrush(new SolidColorBrush(background));

        /// <summary>Returns new properties with the given background brush.</summary>
        public TextFormattingRunProperties SetBackgroundBrush(IBrush brush)
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, brush, _bold, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the given foreground opacity.</summary>
        public TextFormattingRunProperties SetForegroundOpacity(double opacity)
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, _bold, _italic, opacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the given background opacity.</summary>
        public TextFormattingRunProperties SetBackgroundOpacity(double opacity)
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, _bold, _italic, _foregroundOpacity, opacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the given typeface.</summary>
        public TextFormattingRunProperties SetTypeface(Typeface typeface)
            => new TextFormattingRunProperties(typeface, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, _bold, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the given font rendering size.</summary>
        public TextFormattingRunProperties SetFontRenderingEmSize(double renderingSize)
            => new TextFormattingRunProperties(_typeface, renderingSize, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, _bold, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the given font hinting size.</summary>
        public TextFormattingRunProperties SetFontHintingEmSize(double hintingSize)
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, hintingSize, _foregroundBrush, _backgroundBrush, _bold, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the given text decorations.</summary>
        public TextFormattingRunProperties SetTextDecorations(TextDecorationCollection textDecorations)
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, _bold, _italic, _foregroundOpacity, _backgroundOpacity, textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the given culture.</summary>
        public TextFormattingRunProperties SetCultureInfo(CultureInfo cultureInfo)
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, _bold, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, cultureInfo);

        /// <summary>Returns new properties with the bold property cleared (empty).</summary>
        public TextFormattingRunProperties ClearBold()
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, null, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the italic property cleared (empty).</summary>
        public TextFormattingRunProperties ClearItalic()
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, _bold, null, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the foreground brush cleared (empty).</summary>
        public TextFormattingRunProperties ClearForegroundBrush()
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, null, _backgroundBrush, _bold, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the background brush cleared (empty).</summary>
        public TextFormattingRunProperties ClearBackgroundBrush()
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, null, _bold, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the typeface cleared (empty).</summary>
        public TextFormattingRunProperties ClearTypeface()
            => new TextFormattingRunProperties(null, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, _bold, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the font rendering size cleared (empty).</summary>
        public TextFormattingRunProperties ClearFontRenderingEmSize()
            => new TextFormattingRunProperties(_typeface, null, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, _bold, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the font hinting size cleared (empty).</summary>
        public TextFormattingRunProperties ClearFontHintingEmSize()
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, null, _foregroundBrush, _backgroundBrush, _bold, _italic, _foregroundOpacity, _backgroundOpacity, _textDecorations, _cultureInfo);

        /// <summary>Returns new properties with the text decorations cleared (empty).</summary>
        public TextFormattingRunProperties ClearTextDecorations()
            => new TextFormattingRunProperties(_typeface, _fontRenderingEmSize, _fontHintingEmSize, _foregroundBrush, _backgroundBrush, _bold, _italic, _foregroundOpacity, _backgroundOpacity, null, _cultureInfo);

        /// <summary>Determines whether these properties use the same size as <paramref name="other"/>.</summary>
        public bool SameSize(TextFormattingRunProperties other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            return _fontRenderingEmSize == other._fontRenderingEmSize && _fontHintingEmSize == other._fontHintingEmSize;
        }

        #endregion

        /// <summary>Determines whether two instances specify the same formatting.</summary>
        public bool Equals(TextFormattingRunProperties other)
        {
            if (other == null)
                return false;
            return Nullable.Equals(_typeface, other._typeface)
                && _fontRenderingEmSize == other._fontRenderingEmSize
                && _fontHintingEmSize == other._fontHintingEmSize
                && Equals(_foregroundBrush, other._foregroundBrush)
                && Equals(_backgroundBrush, other._backgroundBrush)
                && _bold == other._bold
                && _italic == other._italic
                && _foregroundOpacity == other._foregroundOpacity
                && _backgroundOpacity == other._backgroundOpacity
                && Equals(_textDecorations, other._textDecorations)
                && Equals(_cultureInfo, other._cultureInfo);
        }

        public override bool Equals(object obj) => this.Equals(obj as TextFormattingRunProperties);

        public override int GetHashCode()
        {
            return HashCode.Combine(_typeface, _fontRenderingEmSize, _foregroundBrush, _backgroundBrush, _bold, _italic, _textDecorations, _cultureInfo);
        }

        private static IBrush ApplyOpacity(IBrush brush, double? opacity)
        {
            if (!opacity.HasValue || brush == null || brush.Opacity == opacity.Value)
                return brush;
            if (brush is ISolidColorBrush solid)
                return new SolidColorBrush(solid.Color, opacity.Value);
            if (brush is Brush mutable)
            {
                // Avalonia brushes have no Clone; opacity on non-solid brushes is applied
                // only when the brush instance is private to this run's properties.
                mutable.Opacity = opacity.Value;
            }

            return brush;
        }
    }
}
