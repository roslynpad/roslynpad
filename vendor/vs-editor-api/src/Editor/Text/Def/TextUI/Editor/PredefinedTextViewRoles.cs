//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Specifies the names of the pre-defined text view roles supplied by Visual Studio.
    /// </summary>
    public static class PredefinedTextViewRoles
    {
        // The following are the default text view roles.

        /// <summary>
        /// The predefined Document role. Applies to text views of entities, typically stored in files, that have
        /// a definite first line and last line. This excludes entities such as output logs or textual displays of
        /// data that are presented in a form.
        /// </summary>
        public const string Document = "DOCUMENT";

        /// <summary>
        /// The predefined Structured role. Applies to text views of entities that have internal structure that should
        /// be exposed by editor facilities such as Outlining.
        /// </summary>
        public const string Structured = "STRUCTURED";

        /// <summary>
        /// The predefined Interactive role. Applies to text views with which the user can interact using the mouse and/or
        /// keyboard. Views that are not interactive cannot display a caret or a selection and cannot have keyboard input.
        /// </summary>
        public const string Interactive = "INTERACTIVE";

        /// <summary>
        /// The predefined Editable role. Applies to text views that can be changed using the keyboard.
        /// </summary>
        public const string Editable = "EDITABLE";

        /// <summary>
        /// The predefined Analyzable role. Applies to text views of entities that can be analyzed for errors or
        /// other information (such as "quick info").
        /// </summary>
        public const string Analyzable = "ANALYZABLE";

        /// <summary>
        /// The predefined Zoomable role. Applies to text views of entities that allow the user to perform zooming operations.
        /// </summary>
        public const string Zoomable = "ZOOMABLE";


        // These are the non-default text view roles.

        /// <summary>
        /// The predefined Primary Document role. Applies to text views of documents that are open for mainline editing,
        /// excluding auxiliary views of documents.
        /// </summary>
        public const string PrimaryDocument = "PRIMARYDOCUMENT";

        /// <summary>
        /// The predefined Debuggable role. Applies to text views of entities in which the debugger can display information
        /// at runtime.
        /// </summary>
        public const string Debuggable = "DEBUGGABLE";

        /// <summary>
        /// The predefined role used for the preview window created by the enhanced scroll bar.
        /// </summary>
        public const string PreviewTextView = "ENHANCED_SCROLLBAR_PREVIEW";

        /// <summary>
        /// The predefined role used for text views embedded within a containing text view.
        /// </summary>
        public const string EmbeddedPeekTextView = "EMBEDDED_PEEK_TEXT_VIEW";

        /// <summary>
        /// The predefined role used for code definition windows.
        /// </summary>
        public const string CodeDefinitionView = "CODEDEFINITION";

        /// <summary>
        /// The predefined role used for printable views.
        /// </summary>
        public const string Printable = "PRINTABLE";
    }
}