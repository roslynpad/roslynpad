using System;
using System.Threading.Tasks;

namespace RoslynPad.Utilities
{
    public interface IActionCommand
    {
        void Execute();
        bool CanExecute();
        void RaiseCanExecuteChanged();

        event EventHandler CanExecuteChanged;
    }

    public interface IActionCommand<in T> : IActionCommand
    {
        void Execute(T parameter);
        bool CanExecute(T parameter);
    }

    public class DelegateCommandBase : IActionCommand
    {
        private readonly Action _action;
        private readonly Func<bool> _canExecute;
        private readonly Func<Task> _asyncAction;

        public DelegateCommandBase(Action action, Func<bool> canExecute = null)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public DelegateCommandBase(Func<Task> asyncAction, Func<bool> canExecute = null)
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
                ExecuteAsync();
            }
            else
            {
                _action();
            }
        }

        private async void ExecuteAsync()
        {
            await _asyncAction().ConfigureAwait(true);
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class DelegateCommandBase<T> : IActionCommand<T>
    {
        private readonly Action<T> _action;
        private readonly Func<T, bool> _canExecute;
        private readonly Func<T, Task> _asyncAction;

        public DelegateCommandBase(Action<T> action, Func<T, bool> canExecute = null)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public DelegateCommandBase(Func<T, Task> asyncAction, Func<T, bool> canExecute = null)
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
                ExecuteAsync(parameter);
            }
            else
            {
                _action(parameter);
            }
        }

        private async void ExecuteAsync(T parameter)
        {
            await _asyncAction(parameter).ConfigureAwait(true);
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        void IActionCommand.Execute()
        {
            Execute(default(T));
        }

        bool IActionCommand.CanExecute()
        {
            return CanExecute(default(T));
        }
    }
}