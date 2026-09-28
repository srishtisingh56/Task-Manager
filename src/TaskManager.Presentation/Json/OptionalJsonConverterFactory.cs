// using System.Text.Json;
// using System.Text.Json.Serialization;
// using TaskManager.Application.Common.Models;

// public sealed class OptionalJsonConverterFactory : JsonConverterFactory
// {
//     public override bool CanConvert(Type t) =>
//         t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Optional<>);

//     public override JsonConverter CreateConverter(Type t, JsonSerializerOptions o) =>
//         (JsonConverter)Activator.CreateInstance(
//             typeof(OptionalJsonConverter<>).MakeGenericType(t.GetGenericArguments()[0]))!;
// }

// public sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
// {
//     // A field absent from the body is never read, so it stays default(Optional<T>) with IsSet = false.
//     // A field present (even as null) reaches here and becomes "set", which is how a description gets cleared.
//     public override Optional<T> Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
//         => new(JsonSerializer.Deserialize<T>(ref reader, o)!); // adjust to how your struct builds a "set" value

//     public override void Write(Utf8JsonWriter w, Optional<T> v, JsonSerializerOptions o)
//     {
//         if (v.IsSet) JsonSerializer.Serialize(w, v.Value, o);
//         else w.WriteNullValue();
//     }
// }