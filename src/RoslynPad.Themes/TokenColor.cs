using System.Text.Json.Serialization;

namespace RoslynPad.Themes;

public record TokenColor(
    [property: JsonConverter(typeof(ListOrSingleJsonConverter<string>))] List<string>? Scope,
    TokenColorSettings? Settings
);
