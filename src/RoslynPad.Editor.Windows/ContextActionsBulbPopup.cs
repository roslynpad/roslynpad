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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;

namespace RoslynPad.Editor.Windows
{
    internal sealed class ContextActionsBulbPopup : ExtendedPopup
    {
        private readonly MenuItem _mainItem;
        private bool _isOpen;

        public ContextActionsBulbPopup(UIElement parent) : base(parent)
        {
            UseLayoutRounding = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            StaysOpen = true;
            AllowsTransparency = true;
            _mainItem = new MenuItem
            {
                ItemContainerStyle = CreateItemContainerStyle(),
                Header = new Image
                {
                    Source = TryFindResource("Bulb") as ImageSource,
                    Width = 16,
                    Height = 16
                }
            };
            _mainItem.SubmenuOpened += (sender, args) =>
            {
                if (ReferenceEquals(args.OriginalSource, _mainItem))
                {
                    _isOpen = true;
                    MenuOpened?.Invoke(this, EventArgs.Empty);
                }
            };
            Closed += (sender, args) =>
            {
                if (_isOpen)
                {
                    _isOpen = false;
                    MenuClosed?.Invoke(this, EventArgs.Empty);
                }
            };
            var menu = new Menu
            {
                Background = Brushes.Transparent,
                BorderBrush = _mainItem.BorderBrush,
                BorderThickness = _mainItem.BorderThickness,
                Items = { _mainItem }
            };
            Child = menu;
        }

        public event EventHandler MenuOpened;
        public event EventHandler MenuClosed;

        private Style CreateItemContainerStyle()
        {
            var style = new Style(typeof(MenuItem), TryFindResource(typeof(MenuItem)) as Style);
            style.Setters.Add(new Setter(MenuItem.CommandProperty,
                new Binding { Converter = new ActionCommandConverter(this) }));
            style.Seal();
            return style;
        }

        public IEnumerable<object> ItemsSource
        {
            get => (IEnumerable<object>)_mainItem.ItemsSource;
            set => _mainItem.ItemsSource = value;
        }

        public bool HasItems => _mainItem.HasItems;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
                IsOpenIfFocused = false;
        }

        public void Close()
        {
            IsOpenIfFocused = false;
        }

        public bool IsMenuOpen
        {
            get => _mainItem.IsSubmenuOpen;
            set => _mainItem.IsSubmenuOpen = value;
        }

        public Func<object, ICommand> CommandProvider { get; set; }

        public new void Focus()
        {
            _mainItem.Focus();
        }

        public void OpenAtLineStart(CodeTextEditor editor)
        {
            SetPosition(this, editor, editor.TextArea.Caret.Line, 1);
            VerticalOffset -= 16;
            IsOpenIfFocused = true;
        }

        private static void SetPosition(Popup popup, TextEditor editor, int line, int column, bool openAtWordStart = false)
        {
            var document = editor.Document;
            var offset = document.GetOffset(line, column);
            if (openAtWordStart)
            {
                var wordStart = document.FindPreviousWordStart(offset);
                if (wordStart != -1)
                {
                    var wordStartLocation = document.GetLocation(wordStart);
                    line = wordStartLocation.Line;
                    column = wordStartLocation.Column;
                }
            }
            var caretScreenPos = editor.TextArea.TextView.GetPosition(line, column);
            popup.HorizontalOffset = caretScreenPos.X;
            popup.VerticalOffset = caretScreenPos.Y;
            popup.PlacementTarget = editor.TextArea.TextView;
            popup.Placement = PlacementMode.Relative;
        }

        private class ActionCommandConverter : IValueConverter
        {
            private readonly ContextActionsBulbPopup _owner;

            public ActionCommandConverter(ContextActionsBulbPopup owner)
            {
                _owner = owner;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return _owner.CommandProvider?.Invoke(value);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

    }
}