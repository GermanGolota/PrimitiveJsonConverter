using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Text.Json.Serialization;

namespace PrimitiveJsonConverter.Generator.Tests;

public static class TestHelper
{
    public static Task Verify(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        var runtimePath = typeof(System.Runtime.AmbiguousImplementationException).Assembly.Location.Replace("System.Private.CoreLib", "System.Runtime");
        string netStandardPath = GetNetStandardPath();

        IEnumerable<PortableExecutableReference> references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JsonPrimitiveAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(PrimitiveJsonConverterGenerator).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JsonConverterAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(netStandardPath),
            MetadataReference.CreateFromFile(runtimePath),
        };

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references);

        var generator = new PrimitiveJsonConverterGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation);

        var result = driver.GetRunResult();

        return Verifier
            .Verify(driver)
            .UseDirectory("Snapshots");
    }

    private static string GetNetStandardPath()
    {
        var linqAssemblyLocation = typeof(Enumerable).GetTypeInfo().Assembly.Location;
        var coreDir = Directory.GetParent(linqAssemblyLocation);
        return $"{coreDir!.FullName}{Path.DirectorySeparatorChar}netstandard.dll";
    }
}
