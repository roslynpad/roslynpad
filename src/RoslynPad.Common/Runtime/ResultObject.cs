using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Reflection;

namespace RoslynPad.Runtime
{
    internal sealed class ResultObject : MarshalByRefObject
    {
        private readonly object _o;
        private readonly PropertyDescriptor _property;

        private bool _initialized;
        private string _header;
        private IEnumerable<ResultObject> _children;

        public static ResultObject Create(object o)
        {
            return new ResultObject(o, isRoot: true);
        }

        private ResultObject(object o, PropertyDescriptor property = null, bool isRoot = false)
        {
            _o = o;
            _property = property;
            IsRoot = isRoot;
        }

        private ResultObject(string header, IEnumerable<ResultObject> children)
        {
            _header = header;
            _children = children;
            _initialized = true;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
        
        public override string ToString()
        {
            var builder = new StringBuilder();
            BuildStringRecursive(builder, 0);
            return builder.ToString();
        }

        private void BuildStringRecursive(StringBuilder builder, int level)
        {
            if (!_initialized) return;
            for (var i = 0; i < level; i++)
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
                object value;
                try
                {
                    value = _property.GetValue(_o);
                }
                catch (TargetInvocationException exception)
                {
                    _header = $"{_property.Name} = Threw {exception.InnerException.GetType().Name}";
                    _children = new[] { new ResultObject(exception.InnerException) };
                    return;
                }

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

            var propertyDescriptors = TypeDescriptor.GetProperties(_o);
            var children = propertyDescriptors.Cast<PropertyDescriptor>()
                .Select(p => new ResultObject(_o, p));

            var e = _o as IEnumerable;
            if (e != null)
            {
                var enumerableChildren = e.Cast<object>().Select(x => new ResultObject(x)).ToArray();
                var header = $"<enumerable count={enumerableChildren.Length}>";
                if (propertyDescriptors.Count == 0)
                {
                    _children = enumerableChildren;
                    _header = header;
                    return;
                }
                children = children.Concat(new[] { new ResultObject(header, enumerableChildren) });
            }

            var ex = _o as Exception;
            _header = ex != null ? ex.GetType().FullName + ": " + ex.Message : _o.ToString();
            _children = children.ToArray();
        }
    }
}