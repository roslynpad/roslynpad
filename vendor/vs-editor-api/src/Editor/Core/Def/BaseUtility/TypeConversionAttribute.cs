namespace Microsoft.VisualStudio.Utilities
{
    using System;
    using System.Composition;

    /// <summary>
    /// Marks a class exported with a MEF <see cref="ExportAttribute"/> as a conversion from one type to another.
    /// </summary>
    public sealed class TypeConversionAttribute : SingletonBaseMetadataAttribute
    {
        private readonly Type from;
        private readonly Type to;

        /// <summary>
        /// Creates a new instance of <see cref="TypeConversionAttribute"/>.
        /// </summary>
        /// <param name="fromFullName">The <see cref="Type"/> being converted from.</param>
        /// <param name="toFullName">The <see cref="Type"/> being converted to.</param>
        public TypeConversionAttribute(Type from, Type to)
        {
            this.from = from ?? throw new ArgumentNullException(nameof(from));
            this.to = to ?? throw new ArgumentNullException(nameof(to));
        }

        /// <summary>
        /// The name of the being converted from.
        /// </summary>
        public string FromFullName => this.from.AssemblyQualifiedName;

        /// <summary>
        /// The name of the exact type being converted to.
        /// </summary>
        public string ToFullName => this.to.AssemblyQualifiedName;
    }
}
