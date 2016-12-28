using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace RoslynPad.Editor.Windows
{
    internal class ExtendedPopup : Popup
    {
        private readonly UIElement _parent;

        public ExtendedPopup(UIElement parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            _parent = parent;
        }

        public new bool IsOpen => base.IsOpen;

        private bool _openIfFocused;

        public bool IsOpenIfFocused
        {
            get { return _openIfFocused; }
            set
            {
                if (_openIfFocused != value)
                {
                    _openIfFocused = value;
                    if (value)
                    {
                        _parent.IsKeyboardFocusedChanged += parent_IsKeyboardFocusedChanged;
                    }
                    else {
                        _parent.IsKeyboardFocusedChanged -= parent_IsKeyboardFocusedChanged;
                    }
                    OpenOrClose();
                }
            }
        }

        private void parent_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
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