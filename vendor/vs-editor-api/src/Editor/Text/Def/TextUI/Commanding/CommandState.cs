using System;

namespace Microsoft.VisualStudio.Commanding
{
    /// <summary>
    /// Returned by <see cref="ICommandHandler{T}.GetCommandState(T)"/> and determines the state of the command.
    /// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public struct CommandState
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        /// <summary>
        /// If true, the command state is unspecified and should not be taken into account.
        /// </summary>
        public bool IsUnspecified { get; }

        /// <summary>
        /// If true, the command should be available for execution.
        /// <see cref="IsEnabled"/> and <see cref="IsVisible"/> properties control how the command should be represented in the UI.
        /// </summary>
        public bool IsAvailable { get; }

        /// <summary>
        /// If true, the command should be enabled in the UI.
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// If true, the command should be visible in the UI.
        /// </summary>
        public bool IsVisible { get; }

        /// <summary>
        /// If true, the command should appear as checked (i.e. toggled) in the UI.
        /// </summary>
        public bool IsChecked { get; }

        /// <summary>
        /// If specified, returns the custom text that should be displayed in the UI.
        /// </summary>
        public string DisplayText { get; }

        public CommandState(bool isAvailable = false, bool isChecked = false, string displayText = null, bool isUnspecified = false)
            : this(isAvailable: isAvailable, isUnspecified: isUnspecified, isChecked: isChecked, isEnabled: isAvailable, isVisible: isAvailable, displayText: displayText)
        {
        }

        public CommandState(bool isAvailable, bool isChecked, bool isEnabled, bool isVisible, string displayText = null)
            : this(isAvailable: isAvailable, isUnspecified: false, isChecked: isChecked, isEnabled: isEnabled, isVisible: isVisible, displayText: displayText)
        {
        }

        public CommandState(bool isAvailable, bool isUnspecified, bool isChecked, bool isEnabled, bool isVisible, string displayText)
        {
            Validate(isAvailable, isChecked, isUnspecified, isEnabled, isVisible, displayText);

            this.IsAvailable = isAvailable;
            this.IsChecked = isChecked;
            this.IsUnspecified = isUnspecified;
            this.IsEnabled = isEnabled;
            this.IsVisible = isVisible;
            this.DisplayText = displayText;
        }

        private static void Validate(bool isAvailable, bool isChecked, bool isUnspecified, bool isEnabled, bool isVisible, string displayText)
        {
            if (isUnspecified && (isAvailable || isChecked || isEnabled || isVisible || displayText != null))
            {
                throw new ArgumentException("Unspecified command state cannot be combined with other states or command text.");
            }
        }

        /// <summary>
        /// A helper singleton representing an available (supported, enabled and visible) command state.
        /// </summary>
        public static CommandState Available { get; } = new CommandState(isAvailable: true, isChecked: false, isEnabled: true, isVisible: true);

        /// <summary>
        /// A helper singleton representing an unavailable command state.
        /// </summary>
        public static CommandState Unavailable { get; } = new CommandState(isAvailable: false);

        /// <summary>
        /// A helper singleton representing an unspecified command state.
        /// </summary>
        public static CommandState Unspecified { get; } = new CommandState(isUnspecified: true);
    }
}
