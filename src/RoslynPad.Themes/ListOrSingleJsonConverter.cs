using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoslynPad.Themes;

internal class ListOrSingleJsonConverter<T> : JsonConverter<List<T>>
{
    public override List<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return [];
        }
        
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            return [JsonSerializer.Deserialize<T>(ref reader, options)!];
        }

        return JsonSerializer.Deserialize<List<T>>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options) => throw new NotSupportedException();
}
