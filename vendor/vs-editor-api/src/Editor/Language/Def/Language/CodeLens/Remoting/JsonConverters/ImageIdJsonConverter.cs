using Microsoft;
using System;
using Microsoft.VisualStudio.Core.Imaging;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    internal partial class CodeLensAggregateJsonConverter : JsonConverter
    {
        private sealed class ImageIdJsonConverter : BaseJsonConverter<ImageId>
        {
            protected override ImageId ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                Assumes.True(reader.TokenType == JsonToken.StartObject);

                var guid = ReadProperty<Guid>(reader, serializer);
                var id = ReadProperty<long>(reader);

                Assumes.True(reader.Read());
                Assumes.True(reader.TokenType == JsonToken.EndObject);

                return new ImageId(guid, (int)id);
            }

            protected override void WriteValue(JsonWriter writer, ImageId value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(ImageId.Guid));
                writer.WriteValue(value.Guid);
                writer.WritePropertyName(nameof(ImageId.Id));
                writer.WriteValue(value.Id);
                writer.WriteEndObject();
            }
        }
    }
}
