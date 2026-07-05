//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using Microsoft.VisualStudio.Text.Differencing;

#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    /// <summary>
    /// Options applicable to text editing transactions.
    /// </summary>
    public struct EditOptions
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {

        #region Common EditOptions values

        /// <summary>
        /// Do nothing special with this edit.
        /// </summary>
        public readonly static EditOptions None = new EditOptions();

        /// <summary>
        /// Turn this edit into a minimal change, using line and word string differencing.
        /// </summary>
        public readonly static EditOptions DefaultMinimalChange = 
            new EditOptions(new StringDifferenceOptions() { DifferenceType = StringDifferenceTypes.Line | StringDifferenceTypes.Word });

        #endregion

        /// <summary>
        /// Create a set of edit options for computing a minimal difference,
        /// with the given <see cref="StringDifferenceOptions" />.
        /// </summary>
        public EditOptions(StringDifferenceOptions differenceOptions)
        {
            this.ComputeMinimalChange = true;
            this.DifferenceOptions = differenceOptions;
        }

        /// <summary>
        /// Create a set of edit options.
        /// </summary>
        public EditOptions(bool computeMinimalChange, StringDifferenceOptions differenceOptions)
        {
            this.ComputeMinimalChange = computeMinimalChange;
            this.DifferenceOptions = differenceOptions;
        }

        /// <summary>
        /// True if this edit computes minimal change using the differencing option <see cref="StringDifferenceOptions"/>, false otherwise.
        /// </summary>
        public bool ComputeMinimalChange { get; }

        /// <summary>
        /// The differencing options for this edit, if <see cref="ComputeMinimalChange" /> is true.
        /// </summary>
        /// <remarks>
        /// <see cref="StringDifferenceOptions.IgnoreTrimWhiteSpace" /> will be
        /// ignored.
        /// </remarks>
        public StringDifferenceOptions DifferenceOptions { get; }

        #region Overridden methods and operators

        /// <summary>
        /// Provides a string representation of these edit options.
        /// </summary>
        public override string ToString()
        {
            if (this == EditOptions.None || !this.ComputeMinimalChange)
            {
                return "{none}";
            }
            else
            {
                return DifferenceOptions.ToString();
            }
        }

        /// <summary>
        /// Provides a hash function for the type.
        /// </summary>
        public override int GetHashCode()
        {
            if (this == EditOptions.None || !this.ComputeMinimalChange)
            {
                return 0;
            }
            else
            {
                return DifferenceOptions.GetHashCode();
            }
        }

        /// <summary>
        /// Determines whether two spans are the same.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        public override bool Equals(object obj)
        {
            if (obj is EditOptions)
            {
                EditOptions other = (EditOptions)obj;
                if (other.ComputeMinimalChange != this.ComputeMinimalChange)
                    return false;
                if (!this.ComputeMinimalChange)
                    return true;

                return other.DifferenceOptions == this.DifferenceOptions;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether two EditOptions are the same
        /// </summary>
        public static bool operator ==(EditOptions left, EditOptions right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two EditOptions are different.
        /// </summary>
        public static bool operator !=(EditOptions left, EditOptions right)
        {
            return !(left == right);
        }

        #endregion // Overridden methods and operators
    }
}
