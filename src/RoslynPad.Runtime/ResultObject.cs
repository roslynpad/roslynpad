using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace RoslynPad.Runtime;

internal class ResultObject
{
    private static readonly HashSet<string> s_irrelevantEnumerableProperties = ["Count", "Length", "Key"];

    private static readonly HashSet<string> s_doNotTreatAsEnumerableTypeNames = ["JObject", "JProperty", "JsonObject"];

    private static readonly Dictionary<string, string> s_toStringAlternatives = new()
    {
        ["JArray"] = "[...]",
        ["JObject"] = "{...}",
        ["JsonElement"] = "{...}",
        ["JsonDocument"] = "{...}",
        ["JsonArray"] = "[...]",
        ["JsonObject"] = "{...}",
    };

    private readonly DumpQuotas _quotas;
    private readonly MemberInfo? _member;

    public static ResultObject Create(object? o, in DumpQuotas quotas, string? header = null, int? line = null) =>
        new(o, quotas, header, line);

    internal ResultObject(object? o, in DumpQuotas quotas, string? header = null, int? line = null, MemberInfo? member = null)
    {
        _quotas = quotas;
        _member = member;
        IsExpanded = quotas.MaxExpandedDepth > 0;
        Initialize(o, header);
        LineNumber = line;
    }

    public string? Header { get; private set; }
    public int? LineNumber { get; set; }
    public string? Value { get; protected set; }
    public string? Type { get; private set; }
    public List<ResultObject>? Children { get; private set; }
    public bool HasChildren => Children?.Count > 0;
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

    private void PopulateObject(object? o, string? headerPrefix, in DumpQuotas targetQuotas)
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

                if (IsSpecialEnumerable(type, members))
                {
                    PopulateChildren(o, targetQuotas, members, headerPrefix);
                    var enumerable = new ResultObject(o, targetQuotas, headerPrefix);
                    enumerable.InitializeEnumerable(headerPrefix, e, targetQuotas);
                    Children = (Children ?? Enumerable.Empty<ResultObject>()).Concat([enumerable]).ToList();
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

    private static MemberInfo[] GetMembers(Type type) => ((IEnumerable<MemberInfo>)type.GetRuntimeProperties()
        .Where(m => m.GetMethod?.IsPublic == true && !m.GetMethod.IsStatic))
        .Concat(type.GetRuntimeFields().Where(m => m.IsPublic && !m.IsStatic))
        .OrderBy(m => m.Name)
        .ToArray();

    private static IEnumerable? GetEnumerable(object o, Type type) =>
        o is IEnumerable e && !s_doNotTreatAsEnumerableTypeNames.Contains(type.Name) ? e : null;

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
                    value = exception.StackTrace;
                }
                else
                {
                    value = GetMemberValue(o);

                    if (_member.Name == "TargetSite")
                    {
                        targetQuotas = targetQuotas with { MaxDepth = 0 };
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
            Value = $"Threw {exception.InnerException!.GetType().Name}";
            Children = [ExceptionResultObject.Create(exception.InnerException, _quotas)];
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
        if (o == null)
        {
            return;
        }

        var type = o.GetType();
        SetType(type);
    }

    private void SetType(Type type) => Type = GetTypeName(type);

    private static string GetTypeName(Type type)
    {
        Type? currentType = type;
        var ns = type.Namespace;
        string? typeName = null;
        do
        {
            var currentName = GetSimpleTypeName(type);
            typeName = typeName != null ? currentName + "+" + typeName : currentName;
            currentType = currentType.DeclaringType;
        } while (currentType != null);

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

    private void PopulateChildren(object o, in DumpQuotas targetQuotas, IEnumerable<MemberInfo> properties, string? headerPrefix)
    {
        Header = headerPrefix;
        Value = GetString(o);

        if (o == null)
        {
            return;
        }

        var children = new List<ResultObject>();

        foreach (var property in properties)
        {
            children.Add(new ResultObject(o, targetQuotas, member: property));
        }

        Children = children;
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
                while (count < _quotas.MaxEnumerableLength && enumerator.MoveNext())
                {
                    ++count;
                }

                var hasMore = enumerator.MoveNext() ? "+" : "";
                Value = $"<enumerable Count: {count}{hasMore}>";
            }

        }
        catch (Exception exception)
        {
            Header = _member?.Name;
            Value = $"Threw {exception.GetType().Name}";
            Children = [ExceptionResultObject.Create(exception, _quotas)];
        }
    }

    private void InitializeEnumerable(string? headerPrefix, IEnumerable e, in DumpQuotas targetQuotas)
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
                    item.Type ??= enumerableTypeName;
                    items.Add(item);
                    ++index;
                }

                var hasMore = enumerator.MoveNext() ? "+" : "";
                var groupingInterface = type.ImplementedInterfaces
                    .FirstOrDefault(x => x.IsConstructedGenericType &&
                                         x.GetGenericTypeDefinition() == typeof(IGrouping<,>));
                Value = groupingInterface != null
                    ? $"<grouping Count: {items.Count}{hasMore} Key: {groupingInterface?.GetRuntimeProperty("Key")?.GetValue(e)}>"
                    : $"<enumerable Count: {items.Count}{hasMore}>";
                Children = items;
            }
        }
        catch (Exception exception)
        {
            Header = _member?.Name;
            Value = $"Threw {exception.GetType().Name}";
            Children = [ExceptionResultObject.Create(exception, _quotas)];
        }
    }

    private static bool IsSpecialEnumerable(Type t, IEnumerable<MemberInfo> members) => members.Any(p => !s_irrelevantEnumerableProperties.Contains(p.Name))
               && !typeof(IEnumerator).IsAssignableFrom(t)
               && !t.IsArray
               && t.Namespace?.StartsWith("System.Collections", StringComparison.Ordinal) != true
               && t.Namespace?.StartsWith("System.Linq", StringComparison.Ordinal) != true
               && t.Name.IndexOf("Collection", StringComparison.Ordinal) < 0
               && !t.Name.Equals("JArray", StringComparison.Ordinal);

    private string GetString(object o)
    {
        if (o is Exception exception)
        {
            return exception.Message;
        }

        var typeName = o?.GetType().Name;
        if (typeName != null && s_toStringAlternatives.TryGetValue(typeName, out var value))
        {
            return value;
        }

        var s = o + string.Empty;
        return s.Length > _quotas.MaxStringLength ? s.Substring(0, _quotas.MaxStringLength) : s;
    }
}

internal class ExceptionResultObject : ResultObject
{
    private ExceptionResultObject(Exception exception, in DumpQuotas quotas) : base(exception, quotas)
    {
        Message = exception.Message;

        var stackFrames = new StackTrace(exception, fNeedFileInfo: true).GetFrames() ?? [];
        foreach (var stackFrame in stackFrames)
        {
            if (string.IsNullOrWhiteSpace(stackFrame.GetFileName()) &&
                stackFrame.GetFileLineNumber() is var lineNumber && lineNumber > 0)
            {
                LineNumber = lineNumber;
                break;
            }
        }
    }

    public static ExceptionResultObject Create(Exception exception, DumpQuotas? quotas = null) => new(exception, quotas ?? DumpQuotas.Default);

    public string Message { get; private set; }
}

internal class InputReadRequest
{
}

internal class ProgressResultObject
{
    private ProgressResultObject(double? progress) => Progress = progress;

    public static ProgressResultObject Create(double? progress) => new(progress);

    public double? Progress { get; }
}
