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

        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) =>
                (node is TypeDeclarationSyntax type && type.AttributeLists.Count > 0),
            transform: static (n, _) => (TypeDeclarationSyntax)n.Node
            )
            .Where(_ => _ is not null);

        var compilation = context.CompilationProvider.Combine(provider.Collect());

        context.RegisterSourceOutput(compilation, (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<TypeDeclarationSyntax> nodes)
    {
        //if (Debugger.IsAttached is false) Debugger.Launch();

        var typeSymbols = new List<INamedTypeSymbol>();
        foreach (var node in nodes)
        {
            if (IsValidSyntax(node) && TryGetSymbol(compilation, node, out var symbol) && symbol is not null)
            {
                typeSymbols.Add(symbol);
            }
        }

        var (voMappings, diagnostics) = AnalyzeSymbols(typeSymbols);

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

    private static bool TryGetSymbol(Compilation compilation, TypeDeclarationSyntax node, out INamedTypeSymbol? symbol)
    {
        bool isValid;

        var model = compilation.GetSemanticModel(node.SyntaxTree);

        symbol = (INamedTypeSymbol?)model.GetDeclaredSymbol(node);
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

    private static string[] _supportedTypes = new[]
    {
        nameof(Int16),
        nameof(Int32),
        nameof(Int64),

        nameof(UInt16),
        nameof(UInt32),
        nameof(UInt64),

        nameof(Single),
        nameof(Double),

        nameof(String),
        nameof(Guid)
    };

    private static (IEnumerable<ValueObjectMapping> mappings, IEnumerable<Diagnostic> diagnostics) AnalyzeSymbols(List<INamedTypeSymbol> typeSymbols)
    {
        var diagnostics = new List<Diagnostic>();
        var voMappings = new List<ValueObjectMapping>();

        foreach (var symbol in typeSymbols)
        {
            var members = symbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(_ => _.MethodKind == MethodKind.Conversion)
                .ToArray();

            var symbolType = symbol.Name;

            var interestingTypes = new string[_supportedTypes.Length + 1];
            Array.Copy(_supportedTypes, interestingTypes, _supportedTypes.Length);
            interestingTypes[_supportedTypes.Length] = symbolType;

            var maps = new Dictionary<TypeMap, IMethodSymbol>();

            foreach (var member in members)
            {
                if (
                    member.Parameters is [var param]
                    && interestingTypes.Contains(param.Type.Name)
                    && interestingTypes.Contains(member.ReturnType.Name)
                    )
                {
                    maps.Add(new(param.Type.Name, member.ReturnType.Name), member);
                }
            }

            var used = new List<TypeMap>();
            var mappings = new List<ValueObjectMapping>();
            foreach (var map in maps.Keys)
            {
                var (@in, @out) = map;
                var inverse = new TypeMap(@out, @in);
                if (used.Contains(map) is false
                    && used.Contains(inverse) is false
                    && maps.TryGetValue(inverse, out var path))
                {
                    used.Add(inverse);
                    used.Add(map);
                    var primitive =
                        map.InType == symbolType
                            ? map.OutType
                            : map.InType;
                    //TODO: May need to be updated
                    mappings.Add(new(
                        ToGlobal("System", primitive),
                        ToGlobal(symbol.ContainingNamespace.ToString(), symbolType),
                        symbolType));
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
                diagnostics.Add(Diagnostic.Create(desc, symbol.Locations[0], new[] { symbolType }));
            }
        }

        return (voMappings, diagnostics);
    }

    private static string ToGlobal(string @namespace, string typeName) =>
        @namespace switch
        {
            "<global namespace>" => $"global::{typeName}",
            _ => $"global::{@namespace}.{typeName}"
        };

    private sealed record TypeMap(string InType, string OutType);
}
