using System.Windows.Input;
using Dock.Model.Core;

namespace RoslynPad;

/// <summary>
/// Closes an <see cref="IDockable"/> through its owner's factory. Exists because compiled
/// bindings can't bind a command to <c>IFactory.CloseDockable(IDockable)</c> (typed parameter).
/// </summary>
internal sealed class CloseDockableCommand : ICommand
{
    public static CloseDockableCommand Instance { get; } = new();

    public event EventHandler? CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is IDockable dockable)
        {
            dockable.Owner?.Factory?.CloseDockable(dockable);
        }
    }
}
