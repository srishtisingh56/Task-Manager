using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManager.Application.Common.Models;

public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type t) =>
        t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type t, JsonSerializerOptions o) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(OptionalJsonConverter<>).MakeGenericType(t.GetGenericArguments()[0]))!;
}
public sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Reaching this method at all means the property WAS present in the JSON body
        // (STJ never calls Read for an absent property) — including when its value is null,
        // which correctly counts as "explicitly set to clear the field" per your PATCH semantics.
        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return Optional<T>.Some(value!);
    }

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
    {
        if (value.IsSet)
            JsonSerializer.Serialize(writer, value.Value, options);
        else
            writer.WriteNullValue();
    }
}