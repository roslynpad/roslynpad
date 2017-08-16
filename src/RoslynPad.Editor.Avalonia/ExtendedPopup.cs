using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace RoslynPad.Editor
{
    internal class ExtendedPopup : Popup
    {
        private readonly Control _parent;

        public ExtendedPopup(Control parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public new bool IsOpen => base.IsOpen;

        private bool _openIfFocused;

        public bool IsOpenIfFocused
        {
            get => _openIfFocused; set
            {
                if (_openIfFocused != value)
                {
                    _openIfFocused = value;
                    if (value)
                    {
                        _parent.GotFocus += Parent_IsFocusedChanged;
                        _parent.LostFocus += Parent_IsFocusedChanged;
                    }
                    else
                    {
                        _parent.GotFocus -= Parent_IsFocusedChanged;
                        _parent.LostFocus -= Parent_IsFocusedChanged;
                    }
                    OpenOrClose();
                }
            }
        }

        private void Parent_IsFocusedChanged(object sender, RoutedEventArgs e)
        {
            OpenOrClose();
        }

        private void OpenOrClose()
        {
            var newIsOpen = _openIfFocused && (_parent.IsFocused || IsFocused);
            base.IsOpen = newIsOpen;
        }
    }
}