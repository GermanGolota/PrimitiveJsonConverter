namespace PrimitiveJsonConverterGenerator;

internal static class ConstantCode
{
    public static string FactoryName = "PrimitiveJsonConverterFactory";

    public static string Factory =
""""
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
"""";

    public static string LoaderName = "PrimitiveJsonConverterLoader";

    public static string Loader =
""""
namespace PrimitiveJsonConverterGenerator
{
    internal static partial class PrimitiveJsonConverterLoader
    {
        public static partial void Load();
    }
}
"""";
}