namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Interface that can be used for the <see cref="ITextBuffer.CreateEdit(EditOptions, int?, object)"/> editTag parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface, by itself, does nothing. The derived interfaces, however, can provide some context on the nature of the edit.
    /// For example, the tags for edits associated with the user doing an "undo" should derive from <see cref="IUndoEditTag"/> and
    /// <see cref="IUserEditTag"/>.
    /// </para>
    /// </remarks>
    public interface IEditTag { }

    /// <summary>
    /// Indicates a constraint that no additional edits should be performed in the buffer's <see cref="ITextBuffer.Changed"/> event
    /// handlers called in response to this edit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This constraint is not currently enforced but that may happen in the future.
    /// </para>
    /// </remarks>
    public interface IInviolableEditTag : IEditTag { }

    /// <summary>
    /// Indicates that this edit will create an invisible undo transaction.
    /// </summary>
    public interface IInvisibleEditTag : IEditTag { }

    /// <summary>
    /// Indicates that the edit is part of an undo or redo.
    /// </summary>
    public interface IUndoEditTag : IInviolableEditTag { }

    /// <summary>
    /// Indicates that the edit is part of automatic formatting.
    /// </summary>
    public interface IFormattingEditTag : IInviolableEditTag { }

    /// <summary>
    /// Indicates that the edit is from a remote collaborator.
    /// </summary>
    public interface IRemoteEditTag : IInviolableEditTag, IInvisibleEditTag { }

    /// <summary>
    /// Indicates that the edit is a direct result of a user action (e.g. typing) as opposed to a side-effect (e.g. the
    /// automatic formatting after the user types a semicolon).
    /// </summary>
    public interface IUserEditTag : IEditTag { }

    /// <summary>
    /// Indicates that the edit is something like a "paste" where the modified text should be formatted.
    /// </summary>
    public interface IFormattingNeededEditTag : IEditTag { }

    /// <summary>
    /// Indicates that the edit is the result of the user typing a character.
    /// </summary>
    public interface ITypingEditTag : IUserEditTag { }

    /// <summary>
    /// Indicates that the edit is the result of the user typing hitting a backspace or delete.
    /// </summary>
    public interface IDeleteEditTag : IUserEditTag { }
}
