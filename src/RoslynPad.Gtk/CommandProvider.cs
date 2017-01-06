using System;
using System.Composition;
using System.Threading.Tasks;
using System.Windows.Input;
using RoslynPad.UI;
using RoslynPad.Utilities;

namespace RoslynPad.Gtk
{
    [Export(typeof(ICommandProvider)), Shared]
    internal class CommandProvider : ICommandProvider
    {
        public IActionCommand Create(Action execute, Func<bool> canExecute = null)
        {
            return new DelegateCommand(execute, canExecute);
        }

        public IActionCommand CreateAsync(Func<Task> execute, Func<bool> canExecute = null)
        {
            return new DelegateCommand(execute, canExecute);
        }

        public IActionCommand<T> Create<T>(Action<T> execute, Func<T, bool> canExecute = null)
        {
            return new DelegateCommand<T>(execute, canExecute);
        }

        public IActionCommand<T> CreateAsync<T>(Func<T, Task> execute, Func<T, bool> canExecute = null)
        {
            return new DelegateCommand<T>(execute, canExecute);
        }

        private class DelegateCommand : DelegateCommandBase, ICommand
        {
            public DelegateCommand(Action action, Func<bool> canExecute = null) : base(action, canExecute)
            {
            }

            public DelegateCommand(Func<Task> asyncAction, Func<bool> canExecute = null) : base(asyncAction, canExecute)
            {
            }

            void ICommand.Execute(object parameter)
            {
                Execute();
            }

            bool ICommand.CanExecute(object parameter)
            {
                return CanExecute();
            }
        }

        private class DelegateCommand<T> : DelegateCommandBase<T>, ICommand
        {
            public DelegateCommand(Action<T> action, Func<T, bool> canExecute = null) : base(action, canExecute)
            {
            }

            public DelegateCommand(Func<T, Task> asyncAction, Func<T, bool> canExecute = null) : base(asyncAction, canExecute)
            {
            }

            bool ICommand.CanExecute(object parameter)
            {
                return CanExecute((T)parameter);
            }

            void ICommand.Execute(object parameter)
            {
                Execute((T)parameter);
            }
        }
    }
}