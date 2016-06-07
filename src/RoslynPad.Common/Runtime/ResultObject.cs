using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;

namespace RoslynPad.Runtime
{
    [DataContract(IsReference = true)]
    internal sealed class ResultObject : INotifyPropertyChanged
    {
        private const int MaxDepth = 5;
        private const int MaxStringLength = 10000;
        private const int MaxEnumerableLength = 10000;

        private readonly int _depth;
        private readonly PropertyDescriptor _property;

        public static ResultObject Create(object o, string header = null)
        {
            return new ResultObject(o, 0, header);
        }

        private ResultObject(object o, int depth, string header = null, PropertyDescriptor property = null)
        {
            _depth = depth;
            _property = property;
            Initialize(o, header);
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
            if (Header != null && Value != null)
            {
                builder.Append(" = ");
            }
            builder.Append(Value);
            builder.AppendLine();
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.BuildStringRecursive(builder, level + 1);
                }
            }
        }

        [DataMember]
        public string Header { get; private set; }

        [DataMember]
        public string Value { get; private set; }

        [DataMember]
        public IList<ResultObject> Children { get; private set; }

        public bool HasChildren => Children?.Count > 0;

        private void Initialize(object o, string headerPrefix)
        {
            if (o == null)
            {
                Header = headerPrefix;
                Value = "<null>";
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
                    Header = _property.Name;
                    Value = $"Threw {exception.InnerException.GetType().Name}";
                    Children = new[] { new ResultObject(exception.InnerException, targetDepth) };
                    return;
                }

                var propertyType = _property.PropertyType;
                var isSimpleType = IsSimpleType(propertyType);
                if (!isSimpleType)
                {
                    var enumerable = value as IEnumerable;
                    if (enumerable != null)
                    {
                        InitializeEnumerable(_property.Name, enumerable, targetDepth);
                        return;
                    }
                }
                Header = _property.Name;
                Value = GetString(value);
                if (!isMaxDepth && !isSimpleType)
                {
                    Children = new[] { new ResultObject(value, targetDepth) };
                }
                return;
            }

            var s = o as string;
            if (s != null)
            {
                Header = headerPrefix;
                Value = s;
                return;
            }

            string header;
            string valueString;

            if (isMaxDepth)
            {
                GetHeaderValue(o, out header, out valueString);
                Header = header ?? headerPrefix;
                Value = valueString;
                return;
            }

            var e = o as IEnumerable;
            if (e != null)
            {
                InitializeEnumerable(headerPrefix, e, targetDepth);
                return;
            }

            GetHeaderValue(o, out header, out valueString);
            Header = header ?? headerPrefix;
            Value = valueString;

            var propertyDescriptors = TypeDescriptor.GetProperties(o);
            var children = propertyDescriptors.Cast<PropertyDescriptor>()
                .Select(p => new ResultObject(o, targetDepth, property: p));
            Children = children.ToArray();
        }

        private void InitializeEnumerable(string headerPrefix, IEnumerable e, int targetDepth)
        {
            try
            {
                Header = headerPrefix;
                var items = new List<ResultObject>();
                var enumerator = e.GetEnumerator();
                var index = 0;
                while (index++ < MaxEnumerableLength && enumerator.MoveNext())
                {
                    items.Add(new ResultObject(enumerator.Current, targetDepth));
                }
                var hasMore = enumerator.MoveNext() ? "+" : "";
                var groupingInterface = e.GetType().GetInterfaces()
                        .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IGrouping<,>));
                Value = groupingInterface != null
                    ? $"<grouping Count: {items.Count}{hasMore} Key: {groupingInterface.GetProperty("Key").GetValue(e)}>"
                    : $"<enumerable Count: {items.Count}{hasMore}>";
                Children = items;
            }
            catch (Exception exception)
            {
                Header = _property.Name;
                Value = $"Threw {exception.GetType().Name}";
                Children = new[] { new ResultObject(exception, targetDepth) };
            }
        }

        private static void GetHeaderValue(object o, out string header, out string value)
        {
            var ex = o as Exception;
            if (ex != null)
            {
                header = ex.GetType().FullName;
                value = ex.Message;
            }
            else
            {
                header = null;
                value = GetString(o);
            }
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

        // avoids WPF PropertyDescriptor binding leaks
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { }
            remove { }
        }
    }
}