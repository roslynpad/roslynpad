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
        private const int MaxDepth = 5;
        private const int MaxStringLength = 10000;
        private const int MaxEnumerableLength = 10000;

        private readonly int _depth;
        private readonly PropertyDescriptor _property;

        public static ResultObject Create(object o)
        {
            return new ResultObject(o, 0);
        }

        private ResultObject(object o, int depth, PropertyDescriptor property = null)
        {
            _depth = depth;
            _property = property;
            Initialize(o);
        }

        private ResultObject(string header, IEnumerable<ResultObject> children, int depth)
        {
            _depth = depth;
            Header = header;
            Children = children;
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
            for (var i = 0; i < level; i++)
            {
                builder.Append("  ");
            }
            builder.Append(Header);
            builder.AppendLine();
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.BuildStringRecursive(builder, level + 1);
                }
            }
        }

        public string Header { get; private set; }

        public IEnumerable<ResultObject> Children { get; private set; }

        private void Initialize(object o)
        {
            if (o == null)
            {
                Header = "<null>";
                return;
            }

            var targetDepth = _depth + 1;
            var isMaxDepth = targetDepth >= MaxDepth;

            if (_property != null)
            {
                object value;
                try
                {
                    value = _property.GetValue(o);
                }
                catch (TargetInvocationException exception)
                {
                    Header = $"{_property.Name} = Threw {exception.InnerException.GetType().Name}";
                    Children = new[] { new ResultObject(exception.InnerException, targetDepth) };
                    return;
                }

                Header = _property.Name + " = " + GetString(value);
                var propertyType = _property.PropertyType;
                if (!isMaxDepth && !IsSimpleType(propertyType))
                {
                    Children = new[] { new ResultObject(value, targetDepth) };
                }
                return;
            }

            var s = o as string;
            if (s != null)
            {
                Header = s;
                return;
            }

            if (isMaxDepth)
            {
                Header = GetHeaderValue(o);
                return;
            }

            var propertyDescriptors = TypeDescriptor.GetProperties(o);
            var children = propertyDescriptors.Cast<PropertyDescriptor>()
                .Select(p => new ResultObject(o, targetDepth, p));

            var e = o as IEnumerable;
            if (e != null)
            {
                var enumerableChildren = e.Cast<object>().Take(MaxEnumerableLength).Select(x => new ResultObject(x, targetDepth)).ToArray();
                var header = $"<enumerable count={enumerableChildren.Length}>";
                if (propertyDescriptors.Count == 0)
                {
                    Children = enumerableChildren;
                    Header = header;
                    return;
                }
                children = children.Concat(new[] { new ResultObject(header, enumerableChildren, targetDepth) });
            }

            Header = GetHeaderValue(o);
            Children = children.ToArray();
        }

        private static string GetHeaderValue(object o)
        {
            var ex = o as Exception;
            return ex != null ? ex.GetType().FullName + ": " + ex.Message : GetString(o);
        }

        private static string GetString(object o)
        {
            var s = o + string.Empty;
            return s.Length > MaxStringLength ? s.Substring(0, MaxStringLength) : s;
        }

        private static bool IsSimpleType(Type propertyType)
        {
            return propertyType != null &&
                (propertyType.IsPrimitive ||
                propertyType.IsEnum ||
                propertyType == typeof(string) ||
                propertyType == typeof(Guid) ||
                IsSimpleType(Nullable.GetUnderlyingType(propertyType)));
        }
    }
}