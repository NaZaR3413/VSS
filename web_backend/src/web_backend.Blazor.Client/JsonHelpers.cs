using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace web_backend.Blazor.Client
{
    public class NullableTypeConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
                return false;

            return true;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(NullableConverter<>).MakeGenericType(typeToConvert.GenericTypeArguments[0]);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }
    }

    public class NullableConverter<T> : JsonConverter<Nullable<T>> where T : struct
    {
        public override Nullable<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String && string.IsNullOrEmpty(reader.GetString()))
                return null;

            return JsonSerializer.Deserialize<T>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, Nullable<T> value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}
