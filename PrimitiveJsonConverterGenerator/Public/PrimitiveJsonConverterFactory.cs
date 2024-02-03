using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrimitiveJsonConverterGenerator;

public sealed class PrimitiveJsonConverterFactory : JsonConverterFactory
{
    public static Dictionary<Type, JsonConverter> Converters = new();

    public override bool CanConvert(Type typeToConvert) =>
        Converters.ContainsKey(typeToConvert);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        Converters[typeToConvert];
}