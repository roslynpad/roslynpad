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

        private bool _headerCached;
        private string _header;

        public string Header
        {
            get
            {
                if (!_headerCached)
                {
                    _header = _resultObject.Header;
                    _headerCached = true;
                }
                return _header;
            }
        }

        public IEnumerable<ResultObjectViewModel> Children
            => _resultObject.Children?.Select(x => new ResultObjectViewModel(x));

        private void Copy()
        {
            Clipboard.SetText(_resultObject.ToString());
        }
    }
}