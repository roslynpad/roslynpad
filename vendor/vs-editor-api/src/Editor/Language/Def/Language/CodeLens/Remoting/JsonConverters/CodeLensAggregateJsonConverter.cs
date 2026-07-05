using Microsoft;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Text;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    internal partial class CodeLensAggregateJsonConverter : JsonConverter
    {
        public static readonly CodeLensAggregateJsonConverter Instance = new CodeLensAggregateJsonConverter();

        private readonly ImmutableDictionary<Type, JsonConverter> map;

        private CodeLensAggregateJsonConverter()
        {
            this.map = CreateConverterMap();
        }

        public override bool CanConvert(Type objectType)
            => this.map.ContainsKey(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => this.map[objectType].ReadJson(reader, objectType, existingValue, serializer);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => this.map[value.GetType()].WriteJson(writer, value, serializer);

        partial void AppendJsonConverters(ImmutableDictionary<Type, JsonConverter>.Builder builder);

        private ImmutableDictionary<Type, JsonConverter> CreateConverterMap()
        {
            var builder = ImmutableDictionary.CreateBuilder<Type, JsonConverter>();

            Add(builder, new SpanJsonConverter());
            Add(builder, new NullableJsonConverter<Span>());
            Add(builder, new ImageIdJsonConverter());
            Add(builder, new NullableJsonConverter<ImageId>());
 
            AppendJsonConverters(builder);

            return builder.ToImmutable();
        }

        private static void Add<T>(
            ImmutableDictionary<Type, JsonConverter>.Builder builder,
            BaseJsonConverter<T> converter)
        {
            builder.Add(typeof(T), converter);
        }

        private abstract class BaseJsonConverter<T> : JsonConverter
        {
            public sealed override bool CanConvert(Type objectType)
                => typeof(T) == objectType;

            public sealed override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
                => ReadValue(reader, serializer);

            public sealed override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                => WriteValue(writer, (T)value, serializer);

            protected abstract T ReadValue(JsonReader reader, JsonSerializer serializer);
            protected abstract void WriteValue(JsonWriter writer, T value, JsonSerializer serializer);

            protected static U ReadProperty<U>(JsonReader reader, JsonSerializer serializer)
            {
                // read property
                Assumes.True(reader.Read());
                Assumes.True(reader.TokenType == JsonToken.PropertyName);

                Assumes.True(reader.Read());
                return serializer.Deserialize<U>(reader);
            }

            protected static U ReadProperty<U>(JsonReader reader)
            {
                // read property
                Assumes.True(reader.Read());
                Assumes.True(reader.TokenType == JsonToken.PropertyName);

                Assumes.True(reader.Read());
                return (U)reader.Value;
            }

            protected static U? ReadNullableProperty<U>(JsonReader reader) where U : struct
            {
                // read property
                Assumes.True(reader.Read());
                Assumes.True(reader.TokenType == JsonToken.PropertyName);

                Assumes.True(reader.Read());
                if (reader.TokenType == JsonToken.Null)
                {
                    return default(U?);
                }
                else
                {
                    return (U)reader.Value;
                }
            }

            protected static U? ReadNullableProperty<U>(JsonReader reader, JsonSerializer serializer) where U : struct
            {
                // read property
                Assumes.True(reader.Read());
                Assumes.True(reader.TokenType == JsonToken.PropertyName);

                Assumes.True(reader.Read());
                if (reader.TokenType == JsonToken.Null)
                {
                    return default(U?);
                }
                else
                {
                    return serializer.Deserialize<U>(reader);
                }
            }
        }

        private sealed class NullableJsonConverter<U> : BaseJsonConverter<U?> where U : struct
        {
            protected override U? ReadValue(JsonReader reader, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                    return default(U?);

                return serializer.Deserialize<U>(reader);
            }

            protected override void WriteValue(JsonWriter writer, U? value, JsonSerializer serializer)
            {
                Debug.Assert(value.HasValue);
                if (value.HasValue)
                {
                    serializer.Serialize(writer, value.Value);
                }
                else
                {
                    writer.WriteNull();
                }
            }
        }
    }
}
