namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    public static class ReferenceEntryFieldNames
    {
        /// <summary>
        /// Name for file path field. Expect a string value.
        /// </summary>
        public const string FilePath = "filePath";
        /// <summary>
        /// Name for line number field. Expect an integer value and this is 0-indexed
        /// </summary>
        public const string LineNumber = "lineNumber";
        /// <summary>
        /// Name for colunm number field. Expect an integer value and this is 0-indexed.
        /// </summary>
        public const string ColumnNumber = "columnNumber";
        /// <summary>
        /// Name for the reference text field. Expect a string value.
        /// </summary>
        public const string ReferenceText = "referenceText";
        /// <summary>
        /// Name for the field of reference start position in the reference text. Expect an integer value.
        /// </summary>
        public const string ReferenceStart = "referenceStart";
        /// <summary>
        /// Name for the field of reference end position in the reference text. Expect an integer value.
        /// </summary>
        public const string ReferenceEnd = "referenceEnd";
        /// <summary>
        /// Name for the field of reference long description. This usually is the reference site with fully qualified reference name. Expect a string value.
        /// </summary>
        public const string ReferenceLongDescription = "referenceLongDescription";
        /// <summary>
        /// Name for the field of reference image. Expect a value of ImageId (or null if no image associated with this reference).
        /// </summary>
        public const string ReferenceImageId = "referenceImageId";
        /// <summary>
        /// Name for the field of the second line before reference text. Expect a string value.
        /// </summary>
        public const string TextBeforeReference2 = "textBeforeReference2";
        /// <summary>
        /// Name for the field of the first line before reference text. Expect a string value.
        /// </summary>
        public const string TextBeforeReference1 = "textBeforeReference1";
        /// <summary>
        /// Name for the field of the first line after reference text. Expect a string value.
        /// </summary>
        public const string TextAfterReference1 = "textAfterReference1";
        /// <summary>
        /// Name of the field of the second line after reference text. Expect a string value.
        /// </summary>
        public const string TextAfterReference2 = "textAfterReference2";
    }
}
