using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace PrimitiveJsonConverterGenerator;

[Generator]
internal sealed class PrimitiveJsonConverterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
           $"{ConstantCode.FactoryName}.g.cs",
           SourceText.From(ConstantCode.Factory, Encoding.UTF8)));

        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
           $"{ConstantCode.LoaderName}.g.cs",
           SourceText.From(ConstantCode.Loader, Encoding.UTF8)));
        
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "System.Text.Json.Serialization.JsonConverterAttribute",
                (syntax, _) => true,
                (ctx, _) => (ctx.TargetNode, ctx.TargetSymbol))
            .Where(_ => _.TargetNode is TypeDeclarationSyntax type && IsValidSyntax(type) && _.TargetSymbol is INamedTypeSymbol n && IsValidSymbol(n))
            .Select((pair, _) => (INamedTypeSymbol)pair.TargetSymbol)
            .Select((symbol, _) => new TypeDescription(
                symbol.Name,
                Namespace.From(symbol.ContainingNamespace),
                symbol.Locations[0],
                symbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(_ => _.MethodKind == MethodKind.Conversion)
                    .Where(member => member.Parameters.Length == 1
                        && IsInteresting(member.Parameters[0].Type.Name, symbol.Name)
                        && IsInteresting(member.ReturnType.Name, symbol.Name)
                    )
                    .Select(member => new TypeMap(member.Parameters[0].Type.Name, member.ReturnType.Name))
                    .ToImmutableArray()
                ))
            .WithComparer(new TypeDescriptionComparer());

        var compilation = context.CompilationProvider.Combine(provider.Collect());

        context.RegisterSourceOutput(compilation, (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private static string[] _supportedTypes = new[]
    {
        nameof(Int16),
        nameof(Int32),
        nameof(Int64),

        nameof(Byte),
        nameof(UInt16),
        nameof(UInt32),
        nameof(UInt64),

        nameof(Single),
        nameof(Double),
        nameof(Decimal),

        nameof(String),
        nameof(Guid),

        nameof(Boolean),

        nameof(DateTime),
        nameof(DateTimeOffset)
    };

    private static bool IsInteresting(string typeName, string symbolName) =>
        _supportedTypes.Contains(typeName) || typeName == symbolName;

    internal sealed class TypeDescriptionComparer : IEqualityComparer<TypeDescription>
    {
        bool IEqualityComparer<TypeDescription>.Equals(TypeDescription x, TypeDescription y) =>
            x.TypeName == y.TypeName 
            && x.Namespace == y.Namespace 
            && x.Location == y.Location 
            && x.TypeMaps.SequenceEqual(y.TypeMaps);

        int IEqualityComparer<TypeDescription>.GetHashCode(TypeDescription obj) =>
            obj.GetHashCode();
    }

    private sealed record TypeDescription(string TypeName, Namespace Namespace, Location Location, ImmutableArray<TypeMap> TypeMaps);

    private void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<TypeDescription> descriptions)
    {
        //if (Debugger.IsAttached is false) Debugger.Launch();

        var (voMappings, diagnostics) = AnalyzeSymbols(descriptions);

        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        var classes = new List<string>();
        foreach (var mapping in voMappings)
        {
            var (code, className) = SerializerWriter.WriteCode(mapping);
            context.AddSource($"{className}.g.cs", code);
            classes.Add(className);
        }

        var loaderCode = LoaderWriter.WriteCode(classes);
        context.AddSource($"{ConstantCode.LoaderName}2.g.cs", loaderCode);
    }

    private static bool IsValidSymbol(INamedTypeSymbol symbol)
    {
        bool isValid;

        if (symbol is not null)
        {
            var attributes = symbol.GetAttributes();
            isValid = false;
            foreach (var attribute in attributes)
            {
                if (attribute.ConstructorArguments is [var converterType]
                    && converterType.Value is INamedTypeSymbol converterTypeName
                    && converterTypeName.Name == ConstantCode.FactoryName
                    && converterTypeName.ContainingNamespace.ToString() == "PrimitiveJsonConverterGenerator"
                    && attribute.AttributeClass is not null
                    && attribute.AttributeClass.ContainingNamespace.ToString() == "System.Text.Json.Serialization"
                    )
                {
                    isValid = true;
                    break;
                }
            }
        }
        else
        {
            isValid = false;
        }

        return isValid;
    }

    private static bool IsValidSyntax(TypeDeclarationSyntax node)
    {
        var isProperSyntax = false;
        foreach (var attribute in node.AttributeLists.SelectMany(_ => _.Attributes))
        {
            if (attribute.Name.ToString() == "JsonConverter"
                && attribute.ArgumentList is not null
                && attribute.ArgumentList.Arguments.Count == 1)
            {
                isProperSyntax = true;
                break;
            }
        }

        return isProperSyntax;
    }

    private static Namespace _systemNamespace = new Namespace.Local("System");

    private static (IEnumerable<ValueObjectMapping> mappings, IEnumerable<Diagnostic> diagnostics) AnalyzeSymbols(
        ImmutableArray<TypeDescription> typeDescriptions)
    {
        var diagnostics = new List<Diagnostic>();
        var voMappings = new List<ValueObjectMapping>();

        foreach (var typeDescription in typeDescriptions)
        {
            var used = new List<TypeMap>();
            var mappings = new List<ValueObjectMapping>();
            foreach (var map in typeDescription.TypeMaps)
            {
                var (@in, @out) = map;
                var inverse = new TypeMap(@out, @in);
                if (used.Contains(map) is false
                    && used.Contains(inverse) is false
                    && typeDescription.TypeMaps.Contains(inverse))
                {
                    used.Add(inverse);
                    used.Add(map);
                    var primitive =
                        map.InType == typeDescription.TypeName
                            ? map.OutType
                            : map.InType;

                    mappings.Add(new(
                        _systemNamespace.Format(primitive),
                        typeDescription.Namespace.Format(typeDescription.TypeName),
                        typeDescription.TypeName));
                }
            }

            if (mappings is [var mapping])
            {
                voMappings.Add(mapping);
            }
            else
            {
                var desc = mappings.Count switch
                {
                    0 => Diagnostics.NoConversionOperators,
                    _ => Diagnostics.TooManyConversionOperators
                };
                diagnostics.Add(Diagnostic.Create(desc, typeDescription.Location, new[] { typeDescription.TypeName }));
            }
        }

        return (voMappings, diagnostics);
    }

    private sealed record TypeMap(string InType, string OutType);

    private abstract record Namespace
    {
        public abstract string Format(string typeName);

        public sealed record Global : Namespace
        {
            public override string Format(string typeName) =>
                $"global::{typeName}";
        }

        public sealed record Local(string Value) : Namespace
        {
            public override string Format(string typeName) =>
                $"global::{Value}.{typeName}";
        }

        public static Namespace From(INamespaceSymbol symbol) =>
            symbol.IsGlobalNamespace
            ? new Global()
            : new Local(symbol.ToString());
    }
}
