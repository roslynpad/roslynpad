using Microsoft;
using Microsoft.VisualStudio.Text;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    internal partial class CodeLensAggregateJsonConverter : JsonConverter
    {
        private sealed class SpanJsonConverter : BaseJsonConverter<Span>
        {
            protected override Span ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Assumes.True(reader.TokenType == JsonToken.StartObject);

                // all integer is long
                var start = ReadProperty<long>(reader);
                var length = ReadProperty<long>(reader);

                Assumes.True(reader.Read());
                Assumes.True(reader.TokenType == JsonToken.EndObject);

                return new Span((int)start, (int)length);
            }

            protected override void WriteValue(JsonWriter writer, Span span, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(Span.Start));
                writer.WriteValue(span.Start);
                writer.WritePropertyName(nameof(Span.Length));
                writer.WriteValue(span.Length);
                writer.WriteEndObject();
            }
        }
    }
}
