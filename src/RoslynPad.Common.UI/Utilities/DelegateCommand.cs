using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RoslynPad.Utilities
{
    public interface IDelegateCommand : ICommand
    {
        void Execute();
        bool CanExecute();
        void RaiseCanExecuteChanged();
    }

    public interface IDelegateCommand<in T> : IDelegateCommand
    {
        void Execute(T parameter);
        bool CanExecute(T parameter);
    }

    internal class DelegateCommand : IDelegateCommand
    {
        private readonly Action? _action;
        private readonly Func<bool>? _canExecute;
        private readonly Func<Task>? _asyncAction;

        public DelegateCommand(Action action, Func<bool>? canExecute = null)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public DelegateCommand(Func<Task> asyncAction, Func<bool>? canExecute = null)
        {
            _asyncAction = asyncAction;
            _canExecute = canExecute;
        }

        public bool CanExecute()
        {
            return _canExecute == null || _canExecute();
        }


        public void Execute()
        {
            if (_asyncAction != null)
            {
                var task = _asyncAction();
            }
            else
            {
                _action?.Invoke();
            }
        }

        bool ICommand.CanExecute(object parameter) => CanExecute();

        void ICommand.Execute(object parameter) => Execute();

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal class DelegateCommand<T> : IDelegateCommand<T>
    {
        private readonly Action<T>? _action;
        private readonly Func<T, bool>? _canExecute;
        private readonly Func<T, Task>? _asyncAction;

        public DelegateCommand(Action<T> action, Func<T, bool>? canExecute = null)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public DelegateCommand(Func<T, Task> asyncAction, Func<T, bool>? canExecute = null)
        {
            _asyncAction = asyncAction;
            _canExecute = canExecute;
        }

        public bool CanExecute(T parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(T parameter)
        {
            if (_asyncAction != null)
            {
                var task = _asyncAction(parameter);
            }
            else
            {
                _action?.Invoke(parameter);
            }
        }

        bool ICommand.CanExecute(object parameter) => CanExecute((T)parameter);

        void ICommand.Execute(object parameter) => Execute((T)parameter);

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference or unconstrained type parameter.
        void IDelegateCommand.Execute()
        {
            Execute(default!);
        }

        bool IDelegateCommand.CanExecute()
        {
            return CanExecute(default!);
        }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference or unconstrained type parameter.
    }
}