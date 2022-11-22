using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace RoslynPad.Build;

public interface IResultObject
{
    string? Value { get; }

    void WriteTo(StringBuilder builder);
}

public class ResultObject : IResultObject
{
    [JsonPropertyName("h")]
    public string? Header { get; set; }

    [JsonPropertyName("v")]
    public string? Value { get; set; }

    [JsonPropertyName("t")]
    public string? Type { get; set; }

    [JsonPropertyName("c")]
    public List<ResultObject>? Children { get; set; }

    public bool HasChildren => Children?.Count > 0;

    [JsonPropertyName("x")]
    public bool IsExpanded { get; set; }

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
}

public class ExceptionResultObject : ResultObject
{
    [JsonPropertyName("l")]
    public int LineNumber { get; set; }

    [JsonPropertyName("m")]
    public string? Message { get; set; }
}

public class InputReadRequest
{
}

public class ProgressResultObject
{
    [JsonPropertyName("p")]
    public double? Progress { get; set; }
}

public class CompilationErrorResultObject : IResultObject
{
    public string? ErrorCode { get; set; }
    public string? Severity { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public string? Message { get; set; }

    public static CompilationErrorResultObject Create(string severity, string errorCode, string message, int line, int column) => new()
    {
        ErrorCode = errorCode,
        Severity = severity,
        Message = message,
        // 0 to 1-based
        Line = line + 1,
        Column = column + 1,
    };

    public override string ToString() => $"{ErrorCode}: {Message}";

    string? IResultObject.Value => ToString();

    public void WriteTo(StringBuilder builder) => builder.Append(ToString());
}

public class RestoreResultObject : IResultObject
{
    private readonly string? _value;

    public RestoreResultObject(string message, string severity, string? value = null)
    {
        Message = message;
        Severity = severity;
        _value = value;
    }

    public string Message { get; set; }
    public string Severity { get; set; }
    public string Value => _value ?? Message;

    public void WriteTo(StringBuilder builder) => builder.Append(Value);
}
