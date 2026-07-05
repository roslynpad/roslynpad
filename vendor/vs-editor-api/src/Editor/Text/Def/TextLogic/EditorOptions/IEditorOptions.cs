//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents common editor options and an extensible mechanism for modifying values and adding new options.
    /// </summary>
    public interface IEditorOptions
    {
        /// <summary>
        /// Gets the value of the option identified by the specified option ID.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="optionId">The ID of the option.</param>
        /// <returns>The current value of the option.</returns>
        T GetOptionValue<T>(string optionId);

        /// <summary>
        /// Gets the value of the option identified by the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the option.</param>
        /// <returns>The current value of the option.</returns>
        T GetOptionValue<T>(EditorOptionKey<T> key);

        /// <summary>
        /// Gets the value of the option specified by the option ID.
        /// </summary>
        /// <param name="optionId">The ID of the option.</param>
        /// <returns>The current value of the option, as an object. The caller is responsible for casting the object to the correct type.</returns>
        object GetOptionValue(string optionId);

        /// <summary>
        /// Sets the value of the specified option in the current scope. If the given option is not applicable
        /// in the current scope, it attempts to set the option in the global scope.
        /// </summary>
        /// <param name="optionId">The ID of the option.</param>
        /// <param name="value">The new value of the option.</param>
        void SetOptionValue(string optionId, object value);

        /// <summary>
        /// Sets the value of the specified option in the current scope. If the given option is not applicable
        /// in the current scope, it attempts to set the option in the global scope.
        /// </summary>
        /// <param name="key">The key of the option.</param>
        /// <param name="value">The new value of the option.</param>
        void SetOptionValue<T>(EditorOptionKey<T> key, T value);

        /// <summary>
        /// Determines whether the specified option is defined.
        /// </summary>
        /// <param name="optionId">The ID of the option.</param>
        /// <param name="localScopeOnly"><c>true</c> to search only in this scope, <c>false</c> 
        /// to try parent scopes as well. This setting has no effect if the current scope is global.</param>
        /// <returns><c>true</c> if the option is defined, otherwise <c>false</c>.</returns>
        bool IsOptionDefined(string optionId, bool localScopeOnly);

        /// <summary>
        /// Determines whether the specified editor option is defined.
        /// </summary>
        /// <param name="key">The key of the option.</param>
        /// <param name="localScopeOnly"><c>true</c> to search only in this scope, <c>false</c> 
        /// to try parent scopes as well. This setting has no effect if the current scope is global.</param>
        /// <returns><c>true</c> if the option is defined, otherwise <c>false</c>.</returns>
        bool IsOptionDefined<T>(EditorOptionKey<T> key, bool localScopeOnly);

        /// <summary>
        /// Clear the locally-defined value for the given option.
        /// </summary>
        /// <param name="optionId">The ID of the option.</param>
        /// <returns><c>true</c> if the option was defined locally and cleared.</returns>
        bool ClearOptionValue(string optionId);

        /// <summary>
        /// Clear the locally-defined value for the given option.
        /// </summary>
        /// <param name="key">The key of the option.</param>
        /// <returns><c>true</c> if the option was defined locally and cleared.</returns>
        bool ClearOptionValue<T>(EditorOptionKey<T> key);

        /// <summary>
        /// Gets the supported options.
        /// </summary>
        IEnumerable<EditorOptionDefinition> SupportedOptions { get; }

        /// <summary>
        /// Gets the global options.
        /// </summary>
        /// <remarks>This returns the global <see cref="IEditorOptions"/>, even if
        /// the current scope is global.</remarks>
        IEditorOptions GlobalOptions { get; }

        /// <summary>
        /// Gets or sets the immediate parent of this set of options. If this set of
        /// options has no parent scope (because it is the global scope), this property is null
        /// and cannot be set.
        /// </summary>
        /// <remarks>
        /// When calling set, the new parent must be non-null and a different instance
        /// of IEditorOptions that was created from the same 
        /// <see cref="IEditorOptionsFactoryService" /> as this instance.  Also,
        /// cycles in the Parent chain are not allowed.</remarks>
        IEditorOptions Parent { get; set; }

        /// <summary>
        /// Occurs when any option changes. Options that change in the global scope
        /// cause this event to be raised if they are also applicable to this
        /// scope.
        /// </summary>
        event EventHandler<EditorOptionChangedEventArgs> OptionChanged;
    }
}
