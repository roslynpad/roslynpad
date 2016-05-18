using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using RoslynPad.Runtime;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal sealed class ResultObjectViewModel : NotificationObject
    {
        private readonly ResultObject _resultObject;
        private readonly Lazy<Task> _detailsLoadTask;

        private ResultObjectViewModel[] _children;

        public ResultObjectViewModel(ResultObject resultObject)
        {
            _resultObject = resultObject;
            // use a Task since ResultObject is a MBRO, so each member access means IO
            _detailsLoadTask = new Lazy<Task>(() => Task.Run(() =>
            {
                _header = _resultObject.Header;
                OnPropertyChanged(nameof(Header));
                _value = _resultObject.Value;
                OnPropertyChanged(nameof(Value));
                _children = _resultObject.Children?.Select(x => new ResultObjectViewModel(x)).ToArray();
                OnPropertyChanged(nameof(Children));
            }));
            CopyCommand = new DelegateCommand(Copy);
        }

        public ICommand CopyCommand { get; }

        private string _header;
        public string Header
        {
            get
            {
                // ReSharper disable once UnusedVariable
                var task = _detailsLoadTask.Value;
                return _header;
            }
        }

        private string _value;
        public string Value
        {
            get
            {
                // ReSharper disable once UnusedVariable
                var task = _detailsLoadTask.Value;
                return _value;
            }
        }

        public IEnumerable<ResultObjectViewModel> Children
        {
            get
            {
                // ReSharper disable once UnusedVariable
                var task = _detailsLoadTask.Value;
                return _children;
            }
        }

        private async Task Copy()
        {
            var text = await Task.Run(() => _resultObject.ToString()).ConfigureAwait(true);
            Clipboard.SetText(text);
        }
    }
}