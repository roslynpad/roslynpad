using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using RoslynPad.Utilities;

namespace RoslynPad.Runtime
{
    [DataContract(IsReference = true)]
    [KnownType(typeof(ExceptionResultObject))]
    internal class ResultObject : INotifyPropertyChanged
    {
        private const int MaxDepth = 5;
        private const int MaxStringLength = 10000;
        private const int MaxEnumerableLength = 10000;

        private static readonly ImmutableHashSet<string> _irrelevantEnumerableProperties = ImmutableHashSet<string>.Empty
            .Add("Count").Add("Length").Add("Key");

        private static readonly ImmutableHashSet<string> _doNotTreatAsEnumerableTypeNames = ImmutableHashSet<string>.Empty
            .Add("JObject").Add("JProperty");

        private static readonly ImmutableDictionary<string, string> _toStringAlternatives = ImmutableDictionary<string, string>.Empty
            .Add("JArray", "[...]")
            .Add("JObject", "{...}");

        private readonly int _depth;
        private readonly PropertyDescriptor _property;

        public static ResultObject Create(object o, string header = null)
        {
            return new ResultObject(o, 0, header);
        }

        internal ResultObject(object o, int depth, string header = null, PropertyDescriptor property = null)
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
        public string Type { get; set; }

        [DataMember]
        public IList<ResultObject> Children { get; private set; }

        public bool HasChildren => Children?.Count > 0;

        private void Initialize(object o, string headerPrefix)
        {
            var targetDepth = _depth + 1;

            if (_property != null)
            {
                PopulateProperty(o, targetDepth);
                return;
            }

            PopulateObject(o, headerPrefix, targetDepth);
        }

        private void PopulateObject(object o, string headerPrefix, int targetDepth)
        {
            if (o == null)
            {
                Header = headerPrefix;
                Value = "<null>";
                return;
            }

            var isMaxDepth = targetDepth >= MaxDepth;

            SetType(o);

            if (o is string s)
            {
                Header = headerPrefix;
                Value = s;
                return;
            }

            if (isMaxDepth)
            {
                Header = headerPrefix;
                Value = GetString(o);
                return;
            }

            var properties = GetProperties(o);

            var type = o.GetType();

            var e = GetEnumerable(o, type);
            if (e != null)
            {
                if (IsSpecialEnumerable(type, properties))
                {
                    PopulateChildren(o, targetDepth, properties, headerPrefix);
                    var enumerable = new ResultObject(o, targetDepth, headerPrefix);
                    enumerable.InitializeEnumerable(headerPrefix, e, targetDepth);
                    Children = Children.Concat(new[] { enumerable }).ToArray();
                }
                else
                {
                    InitializeEnumerable(headerPrefix, e, targetDepth);
                }
                return;
            }

            PopulateChildren(o, targetDepth, properties, headerPrefix);
        }

        private IEnumerable GetEnumerable(object o, Type type)
        {
            if (o is IEnumerable e && !_doNotTreatAsEnumerableTypeNames.Contains(type.Name))
            {
                return e;
            }

            return null;
        }

        private void PopulateProperty(object o, int targetDepth)
        {
            object value;
            try
            {
                var exception = o as Exception;
                value = exception != null && _property.Name == nameof(Exception.StackTrace)
                    ? GetStackTrace(exception)
                    : _property.GetValue(o);
            }
            catch (TargetInvocationException exception)
            {
                Header = _property.Name;
                // ReSharper disable once PossibleNullReferenceException
                Value = $"Threw {exception.InnerException.GetType().Name}";
                Children = new[] { new ResultObject(exception.InnerException, targetDepth) };
                return;
            }

            if (value == null)
            {
                SetType(_property.PropertyType);
            }

            PopulateObject(value, _property.Name, targetDepth);
        }

        private void SetType(object o)
        {
            if (o == null) return;

            var type = o.GetType();
            SetType(type);
        }

        private void SetType(Type type)
        {
            Type = GetTypeName(type);
        }

        private static string GetTypeName(Type type)
        {
            var ns = type.Namespace;
            string typeName = null;
            do
            {
                var currentName = GetSimpleTypeName(type);
                typeName = typeName != null ? currentName + "+" + typeName : currentName;
                type = type.DeclaringType;
            } while (type != null);

            typeName = $"{typeName} ({ns})";
            return typeName;
        }

        private static string GetSimpleTypeName(Type type)
        {
            var typeName = type.Name;
            if (type.IsGenericType)
            {
                var separatorIndex = typeName.IndexOf('`');
                if (separatorIndex > 0)
                {
                    typeName = typeName.Substring(0, separatorIndex);
                }
                typeName += "<" + string.Join(", ", type.GenericTypeArguments.Select(GetSimpleTypeName)) + ">";
            }
            return typeName;
        }

        private void PopulateChildren(object o, int targetDepth, PropertyDescriptorCollection properties, string headerPrefix)
        {
            Header = headerPrefix;
            Value = GetString(o);

            if (o == null) return;

            var children = properties.Cast<PropertyDescriptor>()
                .Select(p => new ResultObject(o, targetDepth, property: p));
            Children = children.ToArray();
        }

        private static PropertyDescriptorCollection GetProperties(object o)
        {
            return TypeDescriptor.GetProperties(o);
        }

        protected static string GetStackTrace(Exception exception)
        {
            return GetStackFrames(exception).ToAsyncString();
        }

        protected static IEnumerable<StackFrame> GetStackFrames(Exception exception)
        {
            var frames = new StackTrace(exception, fNeedFileInfo: true).GetFrames();
            if (frames == null || frames.Length == 0)
            {
                return Array.Empty<StackFrame>();
            }
            int index;
            for (index = frames.Length - 1; index >= 0; --index)
            {
                if (IsScriptMethod(frames[index]))
                {
                    break;
                }
            }
            return frames.Take(index + 1);
        }

        protected static bool IsScriptMethod(StackFrame stackFrame)
        {
            return stackFrame.GetMethod()?.DeclaringType?.
                   Assembly.FullName.StartsWith("\u211B", StringComparison.Ordinal) == true;
        }

        private void InitializeEnumerable(string headerPrefix, IEnumerable e, int targetDepth)
        {
            try
            {
                Header = headerPrefix;

                var items = new List<ResultObject>();

                var type = e.GetType();

                var enumerableInterface = type.GetInterfaces()
                        .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                var enumerableType = enumerableInterface?.GenericTypeArguments[0] ?? typeof(object);
                var enumerableTypeName = GetTypeName(enumerableType);

                var enumerator = e.GetEnumerator();
                var index = 0;
                while (index < MaxEnumerableLength && enumerator.MoveNext())
                {
                    var item = new ResultObject(enumerator.Current, targetDepth, $"[{index}]");
                    if (item.Type == null)
                    {
                        item.Type = enumerableTypeName;
                    }
                    items.Add(item);
                    ++index;
                }

                var hasMore = enumerator.MoveNext() ? "+" : "";
                var groupingInterface = type.GetInterfaces()
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
        
        private static bool IsSpecialEnumerable(Type t, PropertyDescriptorCollection properties)
        {
            return properties.OfType<PropertyDescriptor>().Any(p => !_irrelevantEnumerableProperties.Contains(p.Name))
                   && !typeof(IEnumerator).IsAssignableFrom(t)
                   && !t.IsArray
                   && t.Namespace?.StartsWith("System.Collections", StringComparison.Ordinal) != true
                   && t.Namespace?.StartsWith("System.Linq", StringComparison.Ordinal) != true
                   && t.Name.IndexOf("Collection", StringComparison.Ordinal) < 0
                   && !t.Name.Equals("JArray", StringComparison.Ordinal);
        }
        
        private static string GetString(object o)
        {
            if (o is Exception exception)
            {
                return exception.Message;
            }

            var typeName = o?.GetType().Name;
            string value;
            if (typeName != null && _toStringAlternatives.TryGetValue(typeName, out value))
            {
                return value;
            }

            var s = o + string.Empty;
            return s.Length > MaxStringLength ? s.Substring(0, MaxStringLength) : s;
        }

        // avoids WPF PropertyDescriptor binding leaks
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [DataContract(IsReference = true)]
    internal class ExceptionResultObject : ResultObject
    {
        private ExceptionResultObject(Exception exception) : base(exception, 0)
        {
            Message = exception.Message;

            var stackFrames = new StackTrace(exception, fNeedFileInfo: true).GetFrames() ?? Array.Empty<StackFrame>();
            foreach (var stackFrame in stackFrames)
            {
                if (IsScriptMethod(stackFrame))
                {
                    LineNumber = stackFrame.GetFileLineNumber();
                    break;
                }
            }
        }

        public static ExceptionResultObject Create(Exception exception) => new ExceptionResultObject(exception);

        [DataMember]
        public int LineNumber { get; private set; }

        [DataMember]
        public string Message { get; private set; }
    }
}