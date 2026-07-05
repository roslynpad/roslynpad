using Microsoft.VisualStudio.Core.Imaging;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Defines a descriptor representing a reference entry detail for reference indicators.
    /// </summary>
    public sealed class ReferenceDetailEntryDescriptor
    {
        /// <summary>
        /// The full path of the source file where the reference is found.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The line number of the reference.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// The column number of the reference.
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// The content of the line of code where the symbol is referenced.
        /// </summary>
        public string ReferenceText { get; set; }

        /// <summary>
        /// The start position of the reference in the <see cref="ReferenceText"/>.
        /// </summary>
        public int ReferenceStart { get; set; }

        /// <summary>
        /// The end position of the reference in the <see cref="ReferenceText"/>.
        /// </summary>
        public int ReferenceEnd { get; set; }

        /// <summary>
        /// The fully qualified name of the referenced symbol.
        /// </summary>
        public string ReferenceLongDescription { get; set; }    // or use FullyQualifiedSymbolName?

        /// <summary>
        /// The <see cref="ImageId"/> representing the type of the referenced symbol.
        /// </summary>
        public ImageId? ReferenceImageId { get; set; }

        /// <summary>
        /// The content of the second line of code before the reference line.
        /// </summary>
        public string TextBeforeReference2 { get; set; }

        /// <summary>
        /// The content of the first line of code before the reference line.
        /// </summary>
        public string TextBeforeReference1 { get; set; }

        /// <summary>
        /// The content of the first line of code after the reference line.
        /// </summary>
        public string TextAfterReference1 { get; set; }

        /// <summary>
        /// The content of the second line of code after the reference line.
        /// </summary>
        public string TextAfterReference2 { get; set; }
    }
}
