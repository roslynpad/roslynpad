using System;
using System.Threading.Tasks;
using RoslynPad.Utilities;

namespace RoslynPad.UI
{
    public interface ICommandProvider
    {
        IActionCommand Create(Action execute, Func<bool> canExecute = null);
        IActionCommand CreateAsync(Func<Task> execute, Func<bool> canExecute = null);
        IActionCommand<T> Create<T>(Action<T> execute, Func<T, bool> canExecute = null);
        IActionCommand<T> CreateAsync<T>(Func<T, Task> execute, Func<T, bool> canExecute = null);
    }
}