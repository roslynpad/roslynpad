using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RoslynPad.Utilities
{
    internal sealed class DelegateCommand : ICommand
    {
        private readonly Action _action;
        private readonly Func<Task> _asyncAction;

        public DelegateCommand(Action action)
        {
            _action = action;
        }

        public DelegateCommand(Func<Task> asyncAction)
        {
            _asyncAction = asyncAction;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
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

        // ReSharper disable once UnusedMember.Local
        private void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}