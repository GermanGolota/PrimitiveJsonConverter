using System.Reflection;

namespace PrimitiveJsonConverterGenerator;

internal static class VersionHelper
{
    public static string? GeneratorVersion { get; } = GetGeneratorVersion();

    private static string? GetGeneratorVersion()
    {
        var assembly = typeof(SerializerWriter).Assembly;
        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attribute?.InformationalVersion;
    }
}
