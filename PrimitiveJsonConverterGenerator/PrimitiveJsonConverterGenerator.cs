using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace PrimitiveJsonConverterGenerator;

[Generator]
internal sealed class PrimitiveJsonConverterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "PrimitiveJsonConverter.JsonPrimitiveAttribute",
                (syntax, _) => true,
                (ctx, _) => (ctx.TargetNode, ctx.TargetSymbol))
            .Where(_ => _.TargetNode is TypeDeclarationSyntax type
                        && _.TargetSymbol is INamedTypeSymbol n
                        && IsValidSymbol(n))
            .Select((pair, _) => BuildDescription((INamedTypeSymbol)pair.TargetSymbol, (TypeDeclarationSyntax)pair.TargetNode))
            .WithComparer(new TypeDescriptionComparer());

        var compilation = context.CompilationProvider.Combine(provider.Collect());

        context.RegisterSourceOutput(compilation, (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private static TypeDescription BuildDescription(INamedTypeSymbol symbol, TypeDeclarationSyntax syntax) =>
        new TypeDescription(
                symbol.Name,
                Namespace.From(symbol.ContainingNamespace),
                symbol.DeclaredAccessibility,
                syntax.GetTypeKind(),
                syntax.Modifiers.Any(SyntaxKind.PartialKeyword),
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
                );

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
            && x.Accessibility == y.Accessibility
            && x.IsPartial == y.IsPartial
            && x.Kind == y.Kind
            && x.Location == y.Location
            && x.TypeMaps.SequenceEqual(y.TypeMaps);

        int IEqualityComparer<TypeDescription>.GetHashCode(TypeDescription obj) =>
            obj.GetHashCode();
    }

    private sealed record TypeDescription(
        string TypeName,
        Namespace Namespace,
        Accessibility Accessibility,
        TypeDeclarationKind Kind,
        bool IsPartial,
        Location Location,
        ImmutableArray<TypeMap> TypeMaps);

    private void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<TypeDescription> descriptions)
    {
        //if (Debugger.IsAttached is false) Debugger.Launch();

        var (voMappings, diagnostics) = AnalyzeSymbols(descriptions);

        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        foreach (var mapping in voMappings)
        {
            var (code, className) = SerializerWriter.WriteCode(mapping);
            var (voCode, voClassName) = VoWriter.WriteCode(mapping.ClassType, className);
            context.AddSource($"{className}.g.cs", code);
            context.AddSource($"{voClassName}.g.cs", voCode);
        }
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
                if (attribute.AttributeClass is not null
                    && attribute.AttributeClass.ContainingNamespace.ToString() == "PrimitiveJsonConverter"
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
                        new(typeDescription.TypeName, typeDescription.Kind, typeDescription.Namespace, typeDescription.Accessibility),
                        typeDescription.TypeName));
                }
            }

            if (mappings is [var mapping])
            {
                if (typeDescription.IsPartial)
                {
                    voMappings.Add(mapping);
                }
                else
                {
                    diagnostics.Add(Diagnostic.Create(Diagnostics.NotPartial, typeDescription.Location, new[] { typeDescription.TypeName }));
                }
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

}
