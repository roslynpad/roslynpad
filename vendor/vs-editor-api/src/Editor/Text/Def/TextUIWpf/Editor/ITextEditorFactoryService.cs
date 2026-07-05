//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Editor.
//  ITextEditorFactoryService").
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Creates text views on text buffers and hosts for those views.
    /// </summary>
    /// <remarks>This is a MEF component part; import it via [Import].</remarks>
    public interface ITextEditorFactoryService
    {
        /// <summary>
        /// Creates a view on a newly created, empty text buffer of the "text" content type,
        /// with the default roles.
        /// </summary>
        IWpfTextView CreateTextView();

        /// <summary>
        /// Creates a view on the given buffer with the default roles and options.
        /// </summary>
        IWpfTextView CreateTextView(ITextBuffer textBuffer);

        /// <summary>
        /// Creates a view on the given buffer with the given roles and default options.
        /// </summary>
        IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles);

        /// <summary>
        /// Creates a view on the given buffer with the given roles and options.
        /// </summary>
        IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, IEditorOptions parentOptions);

        /// <summary>
        /// Creates a view on a view model built over the given data model.
        /// </summary>
        IWpfTextView CreateTextView(ITextDataModel dataModel, ITextViewRoleSet roles, IEditorOptions parentOptions);

        /// <summary>
        /// Creates a view on the given view model.
        /// </summary>
        IWpfTextView CreateTextView(ITextViewModel viewModel, ITextViewRoleSet roles, IEditorOptions parentOptions);

        /// <summary>
        /// Creates a host (view plus margins) for the given view.
        /// </summary>
        IWpfTextViewHost CreateTextViewHost(IWpfTextView wpfTextView, bool setFocus);

        /// <summary>
        /// Gets the empty role set.
        /// </summary>
        ITextViewRoleSet NoRoles { get; }

        /// <summary>
        /// Gets the set of all predefined text view roles.
        /// </summary>
        ITextViewRoleSet AllPredefinedRoles { get; }

        /// <summary>
        /// Gets the set of roles used when a view is created without an explicit role set.
        /// </summary>
        ITextViewRoleSet DefaultRoles { get; }

        /// <summary>
        /// Creates a role set from the given roles.
        /// </summary>
        ITextViewRoleSet CreateTextViewRoleSet(IEnumerable<string> roles);

        /// <summary>
        /// Creates a role set from the given roles.
        /// </summary>
        ITextViewRoleSet CreateTextViewRoleSet(params string[] roles);

        /// <summary>
        /// Occurs after a text view is created.
        /// </summary>
        event EventHandler<TextViewCreatedEventArgs> TextViewCreated;
    }
}
