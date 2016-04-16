using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using RoslynPad.Runtime;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal sealed class ResultObjectViewModel : NotificationObject
    {
        private readonly ResultObject _resultObject;

        public ResultObjectViewModel(ResultObject resultObject)
        {
            _resultObject = resultObject;

            CopyCommand = new DelegateCommand(() => Copy());
        }

        public ICommand CopyCommand { get; }

        public string Header => _resultObject.Header;

        public IEnumerable<ResultObjectViewModel> Children
            => _resultObject.Children?.Select(x => new ResultObjectViewModel(x));

        private void Copy()
        {
            Clipboard.SetText(_resultObject.ToString());
        }
    }
}