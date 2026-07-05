//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// The definition of an editor option.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with:
    /// [Export(typeof(EditorOptionDefinition))]
    /// </remarks>
    public abstract class EditorOptionDefinition
    {
        /// <summary>
        /// Gets the default value of the option.
        /// </summary>
        /// <remarks> The type of the value must be the same as the <see cref="ValueType"/>.</remarks>
        public abstract object DefaultValue { get; }

        /// <summary>
        /// Gets the actual type of the option. This is used to ensure
        /// that setting the option by using the editor options registry
        /// is type-safe.
        /// </summary>
        public abstract Type ValueType { get; }

        /// <summary>
        /// Gets the name of the option from the options registry.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Determines whether this option is applicable for the given scope (for example, a text buffer).
        /// The default implementation returns <c>true</c>. An option, by default, is applicable to any scope.
        /// </summary>
        /// <remarks>This method will not be called for the global scope. Every option is
        /// valid by definition in the global scope.</remarks>
        public virtual bool IsApplicableToScope(IPropertyOwner scope)
        {
            return true;
        }

        /// <summary>
        /// Determines whether the proposed value is valid.
        /// </summary>
        /// <param name="proposedValue">The proposed value for this option.</param>
        /// <returns><c>true</c> if the value is valid, otherwise <c>false</c>.</returns>
        /// <remarks>By the time the value is passed to this method, it has already
        /// been checked to be of the correct ValueType.
        /// The implementer of this method may modify the value.</remarks>
        public virtual bool IsValid(ref object proposedValue)
        {
            return true;
        }

        #region Object overrides

        /// <summary>
        /// Determines whether two <see cref="EditorOptionDefinition"/> objects are the same.
        /// </summary>
        /// <param name="obj">The object to be compared.</param>
        /// <returns><c>true</c> if the two objects are the same, otherwise <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            var other = obj as EditorOptionDefinition;
            return other != null && string.Equals(other.Name, this.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the hash code of this type.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// Represents the definition of an editor option.
    /// </summary>
    public abstract class EditorOptionDefinition<T> : EditorOptionDefinition
    {
        /// <summary>
        /// Gets the actual type of the option.
        /// </summary>
        public sealed override Type ValueType { get { return typeof(T); } }

        /// <summary>
        /// Gets the name of the option.
        /// </summary>
        public sealed override string Name { get { return this.Key.Name; } }

        /// <summary>
        /// Gets the default value of the option.
        /// </summary>
        public sealed override object DefaultValue { get { return this.Default; } }

        /// <summary>Determines whether the proposed value is valid.
        /// </summary>
        /// <param name="proposedValue">The proposed value for this option.</param>
        /// <returns><c>true</c> if the value is valid, otherwise <c>false</c>.</returns>
        /// <remarks>By the time the value is passed to this method, it has already
        /// been checked to be of the correct ValueType.
        /// The implementer of this method may modify the value.</remarks>
        public sealed override bool IsValid(ref object proposedValue)
        {
            if (proposedValue is T value)
            {
                var result = IsValid(ref value);
                proposedValue = value;

                return result;
            }

            return false;
        }

        /// <summary>
        /// Gets the key of this option.
        /// </summary>
        public abstract EditorOptionKey<T> Key { get; }

        /// <summary>
        /// Gets the default value of this option.
        /// </summary>
        public virtual T Default { get { return default(T); } }

        /// <summary>
        /// Determines whether the proposed value is valid.
        /// </summary>
        /// <param name="proposedValue">The proposed value for this option.</param>
        /// <returns><c>true</c> if the value is valid, otherwise <c>false</c>.</returns>
        /// <remarks>The implementer of this method may modify the value.</remarks>
        public virtual bool IsValid(ref T proposedValue) { return true; }
    }

}
