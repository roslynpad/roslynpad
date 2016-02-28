using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using RoslynPad.Annotations;
using RoslynPad.Utilities;

namespace RoslynPad.Runtime
{
    internal sealed class ResultObject : INotifyPropertyChanged
    {
        private readonly object _o;
        private readonly PropertyDescriptor _property;

        private bool _initialized;
        private string _header;
        private IEnumerable<ResultObject> _children;
        private bool _isExpanded;

        public static ResultObject Create(object o)
        {
            return new ResultObject(o, isRoot: true);
        }

        private ResultObject(object o, PropertyDescriptor property = null, bool isRoot = false)
        {
            _o = o;
            _property = property;
            IsRoot = isRoot;
            CopyCommand = new DelegateCommand((Action)Copy);
        }

        private void Copy()
        {
            var builder = new StringBuilder();
            BuildStringRecursive(builder, 0);
            Clipboard.SetText(builder.ToString());
        }

        private void BuildStringRecursive(StringBuilder builder, int level)
        {
            if (!_initialized) return;
            for (int i = 0; i < level; i++)
            {
                builder.Append("  ");
            }
            builder.Append(_header);
            builder.AppendLine();
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child.BuildStringRecursive(builder, level + 1);
                }
            }
        }

        public ICommand CopyCommand { get; }

        public bool IsRoot { get; }

        public string Header
        {
            get
            {
                Initialize();
                return _header;
            }
        }

        public IEnumerable<ResultObject> Children
        {
            get
            {
                Initialize();
                return _children;
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value == _isExpanded) return;
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (_o == null)
            {
                _header = "<null>";
                return;
            }

            if (_property != null)
            {
                var value = _property.GetValue(_o);
                _header = _property.Name + " = " + value;
                var propertyType = _property.PropertyType;
                if (!propertyType.IsPrimitive &&
                    propertyType != typeof(string) &&
                    !propertyType.IsEnum)
                {
                    _children = new[] { new ResultObject(value) };
                }
                return;
            }

            var s = _o as string;
            if (s != null)
            {
                _header = s;
                return;
            }

            var e = _o as IEnumerable;
            if (e != null)
            {
                var enumerableChildren = e.Cast<object>().Select(x => new ResultObject(x)).ToArray();
                _children = enumerableChildren;
                _header = $"<enumerable count={enumerableChildren.Length}>";
                return;
            }

            var properties = TypeDescriptor.GetProperties(_o).Cast<PropertyDescriptor>()
                .Select(p => new ResultObject(_o, p)).ToArray();
            var ex = _o as Exception;
            _header = ex != null ? (ex.GetType().FullName + ": " + ex.Message) : _o.ToString();
            if (properties.Length > 0)
            {
                _children = properties;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}