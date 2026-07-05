using System;
using Avalonia;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.OptionDescriptions
{
    /// <summary>
    /// Defines the UI associated with an option (or static element on the page).
    /// </summary>
    public interface IOptionControl
    {
        /// <summary>
        /// Get the UI element associated with the option.
        /// </summary>
        /// <remarks>
        /// Will only be called once if the corresponding tools/options page is displayed.
        /// </remarks>
        object Control { get; }

        /// <summary>
        /// Reset the UI element's value to match the option's value.
        /// </summary>
        /// <remarks>
        /// This method should no-op if the control is not associated with an option.
        /// </remarks>
        void Reset();

        /// <summary>
        /// Set the option's value to match the value represented in the UI element.
        /// </summary>
        /// <remarks>
        /// This method should no-op if the control is not associated with an option.
        /// </remarks>
        void Apply();

        /// <summary>
        /// Current value of the option as reflected in the UI and not the underlying <see cref="IEditorOptions"/>.
        /// </summary>
        object GetValue();

        /// <summary>
        /// Raised whenever the value of the option is changed in the UI.
        /// </summary>
        event EventHandler ValueChanged;

        /// <summary>
        /// Indicates whether the children of this control should be enabled.
        /// </summary>
        bool ShouldChildrenBeEnabled { get; }

        event EventHandler ShouldChildrenBeEnabledChanged;
    }
}
