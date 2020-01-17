using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using RoslynPad.Utilities;

namespace RoslynPad.Runtime
{
    internal interface IResultObject
    {
        string? Value { get; }

        void WriteTo(StringBuilder builder);
    }

    [DataContract]
    [KnownType(typeof(ExceptionResultObject))]
    [KnownType(typeof(InputReadRequest))]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
    internal class ResultObject : INotifyPropertyChanged, IResultObject
    {
        private static readonly HashSet<string> _irrelevantEnumerableProperties = new HashSet<string>
            { "Count", "Length", "Key" };

        private static readonly HashSet<string> _doNotTreatAsEnumerableTypeNames = new HashSet<string>
            { "JObject", "JProperty" };

        private static readonly Dictionary<string, string> _toStringAlternatives = new Dictionary<string, string>
        {
            ["JArray"] = "[...]",
            ["JObject"] = "{...}"
        };

        private readonly DumpQuotas _quotas;
        private readonly MemberInfo? _member;

        public static ResultObject Create(object? o, DumpQuotas quotas, string? header = null)
        {
            return new ResultObject(o, quotas, header);
        }

        // for serialization
        protected ResultObject()
        {
        }

        internal ResultObject(object? o, DumpQuotas quotas, string? header = null, MemberInfo? member = null)
        {
            _quotas = quotas;
            _member = member;
            IsExpanded = quotas.MaxExpandedDepth > 0;
            Initialize(o, header);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            BuildStringRecursive(builder, 0);
            return builder.ToString();
        }

        public void WriteTo(StringBuilder stringBuilder)
        {
            BuildStringRecursive(stringBuilder, 0);
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

        [DataMember(Name = "h")]
        public string? Header { get; private set; }

        [DataMember(Name = "v")]
        public string? Value { get; protected set; }

        [DataMember(Name = "t")]
        public string? Type { get; private set; }

        [DataMember(Name = "c")]
        public List<ResultObject>? Children { get; private set; }

        public bool HasChildren => Children?.Count > 0;

        [DataMember(Name = "x")]
        public bool IsExpanded { get; private set; }

        private void Initialize(object? o, string? headerPrefix)
        {
            var targetQuota = _quotas.StepDown();

            if (TryPopulateMember(o, targetQuota))
            {
                return;
            }

            PopulateObject(o, headerPrefix, targetQuota);
        }

        private void PopulateObject(object? o, string? headerPrefix, DumpQuotas targetQuotas)
        {
            if (o == null)
            {
                Header = headerPrefix;
                Value = "<null>";
                return;
            }

            var isMaxDepth = _quotas.MaxDepth <= 0;

            SetType(o);

            if (o is string s)
            {
                Header = headerPrefix;
                Value = s;
                return;
            }

            var type = o.GetType();

            var e = GetEnumerable(o, type);
            if (e != null)
            {
                if (isMaxDepth)
                {
                    InitializeEnumerableHeaderOnly(headerPrefix, e);
                }
                else
                {
                    var members = GetMembers(type);

                    // ReSharper disable once PossibleMultipleEnumeration
                    if (IsSpecialEnumerable(type, members))
                    {
                        // ReSharper disable once PossibleMultipleEnumeration
                        PopulateChildren(o, targetQuotas, members, headerPrefix);
                        var enumerable = new ResultObject(o, targetQuotas, headerPrefix);
                        enumerable.InitializeEnumerable(headerPrefix, e, targetQuotas);
                        Children = Children.Concat(new[] { enumerable }).ToList();
                    }
                    else
                    {
                        InitializeEnumerable(headerPrefix, e, targetQuotas);
                    }
                }
                return;
            }

            if (isMaxDepth)
            {
                Header = headerPrefix;
                Value = GetString(o);
                return;
            }

            PopulateChildren(o, targetQuotas, GetMembers(type), headerPrefix);
        }

        private static MemberInfo[] GetMembers(Type type)
        {
            return ((IEnumerable<MemberInfo>)type.GetRuntimeProperties()
                    .Where(m => m.GetMethod?.IsPublic == true && !m.GetMethod.IsStatic))
                .Concat(type.GetRuntimeFields().Where(m => m.IsPublic && !m.IsStatic))
                .OrderBy(m => m.Name)
                .ToArray();
        }

        private IEnumerable? GetEnumerable(object o, Type type)
        {
            if (o is IEnumerable e && !_doNotTreatAsEnumerableTypeNames.Contains(type.Name))
            {
                return e;
            }

            return null;
        }

        private bool TryPopulateMember(object? o, DumpQuotas targetQuotas)
        {
            if (_member == null)
            {
                return false;
            }

            object? value;
            try
            {
                if (o is Exception exception)
                {
                    if (_member.Name == nameof(Exception.StackTrace))
                    {
                        value = GetStackTrace(exception);
                    }
                    else
                    {
                        value = GetMemberValue(o);

                        if (_member.Name == "TargetSite")
                        {
                            targetQuotas = targetQuotas.WithMaxDepth(0);
                        }
                    }
                }
                else
                {
                    value = GetMemberValue(o);
                }
            }
            catch (TargetInvocationException exception)
            {
                Header = _member.Name;
                // ReSharper disable once PossibleNullReferenceException
                Value = $"Threw {exception.InnerException.GetType().Name}";
                Children = new List<ResultObject> { ExceptionResultObject.Create(exception.InnerException, _quotas) };
                return true;
            }

            if (value == null)
            {
                if (_member is PropertyInfo propertyInfo)
                {
                    SetType(propertyInfo.PropertyType);
                }
                else if (_member is FieldInfo fieldInfo)
                {
                    SetType(fieldInfo.FieldType);
                }
            }

            PopulateObject(value, _member.Name, targetQuotas);
            return true;
        }

        private object? GetMemberValue(object? o)
        {
            object? value = null;

            try
            {
                if (_member is PropertyInfo propertyInfo)
                {
                    if (propertyInfo.GetIndexParameters().Length == 0)
                    {
                        value = propertyInfo.GetValue(o);
                    }
                }
                else if (_member is FieldInfo fieldInfo)
                {
                    value = fieldInfo.GetValue(o);
                }
            }
            catch (Exception ex)
            {
                return ex is TargetInvocationException tiex ? tiex.InnerException : ex;
            }

            return value;
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
            string? typeName = null;
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
            if (type.IsConstructedGenericType)
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

        private void PopulateChildren(object o, DumpQuotas targetQuotas, IEnumerable<MemberInfo> properties, string? headerPrefix)
        {
            Header = headerPrefix;
            Value = GetString(o);

            if (o == null) return;

            var children = properties
                .Select(p => new ResultObject(o, targetQuotas, member: p));
            Children = children.ToList();
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
                   Assembly.FullName.StartsWith("rp-", StringComparison.Ordinal) == true;
        }

        private void InitializeEnumerableHeaderOnly(string? headerPrefix, IEnumerable e)
        {
            Header = headerPrefix;

            try
            {
                var count = 0;
                var enumerator = e.GetEnumerator();
                using (enumerator as IDisposable)
                {
                    while (count < _quotas.MaxEnumerableLength && enumerator.MoveNext()) ++count;
                    var hasMore = enumerator.MoveNext() ? "+" : "";
                    Value = $"<enumerable Count: {count}{hasMore}>";
                }

            }
            catch (Exception exception)
            {
                Header = _member?.Name;
                Value = $"Threw {exception.GetType().Name}";
                Children = new List<ResultObject> { ExceptionResultObject.Create(exception, _quotas) };
            }
        }

        private void InitializeEnumerable(string? headerPrefix, IEnumerable e, DumpQuotas targetQuotas)
        {
            try
            {
                Header = headerPrefix;

                var items = new List<ResultObject>();

                var type = e.GetType().GetTypeInfo();

                var enumerableInterface = type.ImplementedInterfaces
                        .FirstOrDefault(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                var enumerableType = enumerableInterface?.GenericTypeArguments[0] ?? typeof(object);
                var enumerableTypeName = GetTypeName(enumerableType);

                var enumerator = e.GetEnumerator();
                using (enumerator as IDisposable)
                {
                    var index = 0;
                    while (index < _quotas.MaxEnumerableLength && enumerator.MoveNext())
                    {
                        var item = new ResultObject(enumerator.Current, targetQuotas, $"[{index}]");
                        if (item.Type == null)
                        {
                            item.Type = enumerableTypeName;
                        }
                        items.Add(item);
                        ++index;
                    }

                    var hasMore = enumerator.MoveNext() ? "+" : "";
                    var groupingInterface = type.ImplementedInterfaces
                        .FirstOrDefault(x => x.IsConstructedGenericType &&
                                             x.GetGenericTypeDefinition() == typeof(IGrouping<,>));
                    Value = groupingInterface != null
                        ? $"<grouping Count: {items.Count}{hasMore} Key: {groupingInterface.GetRuntimeProperty("Key").GetValue(e)}>"
                        : $"<enumerable Count: {items.Count}{hasMore}>";
                    Children = items;
                }
            }
            catch (Exception exception)
            {
                Header = _member?.Name;
                Value = $"Threw {exception.GetType().Name}";
                Children = new List<ResultObject> { ExceptionResultObject.Create(exception, _quotas) };
            }
        }

        private static bool IsSpecialEnumerable(Type t, IEnumerable<MemberInfo> members)
        {
            return members.Any(p => !_irrelevantEnumerableProperties.Contains(p.Name))
                   && !typeof(IEnumerator).IsAssignableFrom(t)
                   && !t.IsArray
                   && t.Namespace?.StartsWith("System.Collections", StringComparison.Ordinal) != true
                   && t.Namespace?.StartsWith("System.Linq", StringComparison.Ordinal) != true
                   && t.Name.IndexOf("Collection", StringComparison.Ordinal) < 0
                   && !t.Name.Equals("JArray", StringComparison.Ordinal);
        }

        private string GetString(object o)
        {
            if (o is Exception exception)
            {
                return exception.Message;
            }

            var typeName = o?.GetType().Name;
            if (typeName != null && _toStringAlternatives.TryGetValue(typeName, out var value))
            {
                return value;
            }

            var s = o + string.Empty;
            return s.Length > _quotas.MaxStringLength ? s.Substring(0, _quotas.MaxStringLength) : s;
        }

        // avoids WPF PropertyDescriptor binding leaks
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [DataContract]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
    internal class ExceptionResultObject : ResultObject
    {
        // for serialization
        // ReSharper disable once UnusedMember.Local
        private ExceptionResultObject()
        {
            Message = string.Empty;
        }

        private ExceptionResultObject(Exception exception, DumpQuotas quotas) : base(exception, quotas)
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

        public static ExceptionResultObject Create(Exception exception, DumpQuotas? quotas = null) => new ExceptionResultObject(exception, quotas ?? DumpQuotas.Default);

        [DataMember(Name = "l")]
        public int LineNumber { get; private set; }

        [DataMember(Name = "m")]
        public string Message { get; private set; }
    }

    [DataContract]
    internal class InputReadRequest : ResultObject
    {
        public InputReadRequest()
        {
        }
    }

    [DataContract]
    internal class ProgressResultObject: ResultObject
    {
        // for serialization
        // ReSharper disable once UnusedMember.Local
        private ProgressResultObject()
        {
        }

        private ProgressResultObject(double? progress)
        {
            Progress = progress;
        }

        public static ProgressResultObject Create(double? progress) => new ProgressResultObject(progress);

        [DataMember(Name = "p")]
        public double? Progress { get; private set; }
    }

    [DataContract]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
    internal class CompilationErrorResultObject : IResultObject
    {
        // for serialization
        // ReSharper disable once UnusedMember.Local
        protected CompilationErrorResultObject()
        {
            ErrorCode = string.Empty;
            Severity = string.Empty;
            Message = string.Empty;
        }

        [DataMember(Name = "ec")]
        public string ErrorCode { get; private set; }
        [DataMember(Name = "sev")]
        public string Severity { get; private set; }
        [DataMember(Name = "l")]
        public int Line { get; private set; }
        [DataMember(Name = "col")]
        public int Column { get; private set; }
        [DataMember(Name = "m")]
        public string Message { get; private set; }

        public static CompilationErrorResultObject Create(string severity, string errorCode, string message, int line, int column)
        {
            return new CompilationErrorResultObject
            {
                ErrorCode = errorCode,
                Severity = severity,
                Message = message,
                // 0 to 1-based
                Line = line + 1,
                Column = column + 1,
            };
        }

        public override string ToString() => $"{ErrorCode}: {Message}";

        string? IResultObject.Value => ToString();

        public void WriteTo(StringBuilder builder) => builder.Append(ToString());
    }

    internal class RestoreResultObject : IResultObject
    {
        private readonly string? _value;

        public RestoreResultObject(string message, string severity, string? value = null)
        {
            Message = message;
            Severity = severity;
            _value = value;
        }

        public string Message { get; }
        public string Severity { get; }
        public string Value => _value ?? Message;

        public void WriteTo(StringBuilder builder)
        {
            builder.Append(Value);
        }
    }
}