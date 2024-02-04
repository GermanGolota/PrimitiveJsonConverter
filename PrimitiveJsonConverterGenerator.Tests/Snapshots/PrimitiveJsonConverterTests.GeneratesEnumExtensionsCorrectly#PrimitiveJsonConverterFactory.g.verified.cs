//HintName: PrimitiveJsonConverterFactory.g.cs
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable
namespace PrimitiveJsonConverterGenerator
{
    public sealed class PrimitiveJsonConverterFactory : JsonConverterFactory
    {
        static PrimitiveJsonConverterFactory()
        {
            PrimitiveJsonConverterLoader.Load();
        }

        internal static Dictionary<Type, JsonConverter> Converters = new();

        public override bool CanConvert(Type typeToConvert)
        {
            return Converters.ContainsKey(typeToConvert);
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return Converters[typeToConvert];
        }
    }
}
#nullable disable