using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace RoslynPad.Editor
{
    internal class ExtendedPopup : Popup
    {
        private readonly UIElement _parent;

        public ExtendedPopup(UIElement parent)
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
                        _parent.IsKeyboardFocusedChanged += Parent_IsKeyboardFocusedChanged;
                    }
                    else
                    {
                        _parent.IsKeyboardFocusedChanged -= Parent_IsKeyboardFocusedChanged;
                    }
                    OpenOrClose();
                }
            }
        }

        private void Parent_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            OpenOrClose();
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);
            OpenOrClose();
        }

        private void OpenOrClose()
        {
            var newIsOpen = _openIfFocused && (_parent.IsKeyboardFocused || IsKeyboardFocusWithin);
            base.IsOpen = newIsOpen;
        }
    }
}