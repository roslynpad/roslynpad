namespace Microsoft.VisualStudio.Text.Adornments
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    /// <summary>
    /// Represents a container of zero or more elements for display in an <see cref="IToolTipPresenter"/>.
    /// </summary>
    /// <remarks>
    /// Elements are translated to platform-specific UI constructs via the <see cref="IViewElementFactoryService"/>.
    /// </remarks>
    public sealed class ContainerElement
    {
        /// <summary>
        /// Constructs a new container.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="elements"/> is <c>null</c>.</exception>
        /// <param name="style">The layout style for the container.</param>
        /// <param name="elements">The <see cref="IViewElementFactoryService"/> elements to display.</param>
        public ContainerElement(ContainerElementStyle style, IEnumerable<object> elements)
        {
            this.Style = style;
            this.Elements = elements?.ToImmutableList() ?? throw new ArgumentNullException(nameof(elements));
        }

        /// <summary>
        /// Constructs a new container.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="elements"/> is <c>null</c>.</exception>
        /// <param name="style">The layout style for the container.</param>
        /// <param name="elements">The elements to translate to UI and display via the <see cref="IViewElementFactoryService"/>.</param>
        public ContainerElement(ContainerElementStyle style, params object[] elements)
        {
            this.Style = style;
            this.Elements = elements?.ToImmutableList() ?? throw new ArgumentNullException(nameof(elements));
        }

        /// <summary>
        /// The elements to be displayed in the container.
        /// </summary>
        public IEnumerable<object> Elements { get; }

        /// <summary>
        /// The layout style for the container.
        /// </summary>
        public ContainerElementStyle Style { get; }
    }
}
