// Copyright © 2025 Roby Van Damme.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotBump.Commands.BumpTools;

public class StringOrStringArrayConverter : JsonConverter<IEnumerable<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string value = reader.GetString();

            // Handle empty string
            if (string.IsNullOrEmpty(value))
                return new List<string>();

            // Or if you want to treat it as a single item:
            // return new List<string> { value };
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            List<string> list = new List<string>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    list.Add(reader.GetString());
                }
            }

            return list;
        }

        throw new JsonException("Expected string or array of strings");
    }

    public override void Write(Utf8JsonWriter writer, IEnumerable<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (string item in value)
        {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();
    }
}
