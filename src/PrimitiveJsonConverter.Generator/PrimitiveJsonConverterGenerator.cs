using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;

namespace PrimitiveJsonConverter.Generator;

[Generator]
internal sealed class PrimitiveJsonConverterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "PrimitiveJsonConverter.JsonPrimitiveAttribute",
                (syntax, _) => true,
                (ctx, _) => (ctx.TargetNode, ctx.TargetSymbol))
            .Where(_ => _.TargetNode is TypeDeclarationSyntax type
                        && _.TargetSymbol is INamedTypeSymbol n)
            .Select((pair, _) => BuildDescription((INamedTypeSymbol)pair.TargetSymbol, (TypeDeclarationSyntax)pair.TargetNode))
            .WithComparer(new TypeDescriptionComparer());

        var converterProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "PrimitiveJsonConverter.JsonPrimitiveConverterAttribute",
                (syntax, _) => true,
                (ctx, _) => (ctx.TargetNode, ctx.TargetSymbol, ctx.Attributes))
            .Where(_ => _.TargetNode is TypeDeclarationSyntax type
                        && _.TargetSymbol is INamedTypeSymbol n
                        && _.Attributes is [var attribute]
                           && attribute.ConstructorArguments is [var vo]
                           && vo.Kind == TypedConstantKind.Type
                           && vo.Value is INamedTypeSymbol voType
                           && voType.DeclaringSyntaxReferences.Select(_ => _.GetSyntax()).OfType<TypeDeclarationSyntax>().Any()
                  )
            .Select((pair, _) =>
                BuildConverterDescription(
                    (INamedTypeSymbol)pair.TargetSymbol,
                    (TypeDeclarationSyntax)pair.TargetNode,
                    (INamedTypeSymbol)pair.Attributes[0].ConstructorArguments[0].Value!)
                )
            .WithComparer(new ConverterTypeDescriptionComparer());

        var classCompilation = context.CompilationProvider.Combine(classProvider.Collect());
        context.RegisterSourceOutput(classCompilation, (spc, source) => ExecuteClass(spc, source.Left, source.Right));

        var converterCompilation = context.CompilationProvider.Combine(converterProvider.Collect());
        context.RegisterSourceOutput(converterCompilation, (spc, source) => ExecuteConverter(spc, source.Left, source.Right));
    }

    private static ConverterTypeDescription BuildConverterDescription(
        INamedTypeSymbol converterSymbol,
        TypeDeclarationSyntax converterSyntax,
        INamedTypeSymbol voSymbol)
    {
        var voSyntax = voSymbol.DeclaringSyntaxReferences.Select(_ => _.GetSyntax()).OfType<TypeDeclarationSyntax>().First();
        var voType = BuildDescription(voSymbol, voSyntax);
        var converterType = BuildDescription(converterSymbol, converterSyntax);
        return new(voType, converterType.Type, converterType.IsPartial, converterType.Location);
    }

    private static TypeDescription BuildDescription(INamedTypeSymbol symbol, TypeDeclarationSyntax syntax) =>
        new TypeDescription(
                GetTypeInfo(symbol, syntax),
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

    private static TypeDto GetTypeInfo(INamedTypeSymbol symbol, TypeDeclarationSyntax syntax) =>
         new(symbol.Name,
             syntax.GetTypeKind(),
             Namespace.From(symbol.ContainingNamespace),
             symbol.DeclaredAccessibility
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

    private void ExecuteConverter(SourceProductionContext context, Compilation compilation, ImmutableArray<ConverterTypeDescription> descriptions)
    {
        //if (Debugger.IsAttached is false) Debugger.Launch();

        var (voMappings, diagnostics) = AnalyzeSymbols(descriptions);
        ProcessMappings(context, voMappings, diagnostics);
    }

    private void ExecuteClass(SourceProductionContext context, Compilation compilation, ImmutableArray<TypeDescription> descriptions)
    {
        //if (Debugger.IsAttached is false) Debugger.Launch();

        var (voMappings, diagnostics) = AnalyzeSymbols(descriptions);
        ProcessMappings(context, voMappings, diagnostics);
    }

    private static void ProcessMappings(SourceProductionContext context, IEnumerable<ValueObjectMapping> voMappings, IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        foreach (var mapping in voMappings)
        {
            var (code, className) = SerializerWriter.WriteCode(mapping);
            context.AddSource($"{className}.g.cs", code);

            if (mapping.ConverterType is JsonConverterType.None)
            {
                var (voCode, voClassName) = VoWriter.WriteCode(mapping.ClassType, className);
                context.AddSource($"{voClassName}.g.cs", voCode);
            }
        }
    }

    private static Namespace _systemNamespace = new Namespace.Local("System");

    private static (IEnumerable<ValueObjectMapping> mappings, IEnumerable<Diagnostic> diagnostics) AnalyzeSymbols(
        ImmutableArray<TypeDescription> typeDescriptions)
    {
        var diagnostics = new List<Diagnostic>();
        var voMappings = new List<ValueObjectMapping>();

        foreach (var typeDescription in typeDescriptions)
        {
            var mappings = FindMappings(typeDescription, converterTypeDescription: null);
            AnalyzeMappings(typeDescription, converterTypeDescription: null, mappings, diagnostics, voMappings);
        }

        return (voMappings, diagnostics);
    }

    private static (IEnumerable<ValueObjectMapping> mappings, IEnumerable<Diagnostic> diagnostics) AnalyzeSymbols(
      ImmutableArray<ConverterTypeDescription> typeDescriptions)
    {
        var diagnostics = new List<Diagnostic>();
        var voMappings = new List<ValueObjectMapping>();

        foreach (var typeDescription in typeDescriptions)
        {
            var mappings = FindMappings(typeDescription.ClassType, typeDescription);
            AnalyzeMappings(typeDescription.ClassType, typeDescription, mappings, diagnostics, voMappings);
        }

        return (voMappings, diagnostics);
    }

    private static void AnalyzeMappings(
        TypeDescription typeDescription,
        ConverterTypeDescription? converterTypeDescription,
        List<ValueObjectMapping> mappings,
        List<Diagnostic> diagnostics,
        List<ValueObjectMapping> voMappings)
    {
        if (mappings is [var mapping])
        {
            switch (mapping.ConverterType)
            {
                case JsonConverterType.None:
                    if (typeDescription.IsPartial)
                    {
                        voMappings.Add(mapping);
                    }
                    else
                    {
                        diagnostics.Add(Diagnostic.Create(Diagnostics.NotPartialClass, typeDescription.Location, new[] { typeDescription.Type.Name }));
                    }
                    break;

                case JsonConverterType.Some s when converterTypeDescription is not null:
                    if (s.IsPartial && s.Type.Kind == TypeDeclarationKind.Class)
                    {
                        voMappings.Add(mapping);
                    }
                    else
                    {
                        if (s.Type.Kind != TypeDeclarationKind.Class)
                        {
                            diagnostics.Add(Diagnostic.Create(Diagnostics.NotClassConverter, converterTypeDescription.ConverterLocation, new[] { s.Type.Name }));
                        }

                        if (s.IsPartial is false)
                        {
                            diagnostics.Add(Diagnostic.Create(Diagnostics.NotPartialConverter, converterTypeDescription.ConverterLocation, new[] { s.Type.Name }));
                        }
                    }
                    break;
            }
        }
        else
        {
            var desc = mappings.Count switch
            {
                0 => Diagnostics.NoConversionOperators,
                _ => Diagnostics.TooManyConversionOperators
            };
            diagnostics.Add(Diagnostic.Create(desc, typeDescription.Location, new[] { typeDescription.Type.Name }));
        }
    }

    private static List<ValueObjectMapping> FindMappings(TypeDescription typeDescription, ConverterTypeDescription? converterTypeDescription)
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
                    map.InType == typeDescription.Type.Name
                        ? map.OutType
                        : map.InType;

                mappings.Add(new(
                    _systemNamespace.Format(primitive),
                    typeDescription.Type,
                    converterTypeDescription is not null
                        ? new JsonConverterType.Some(converterTypeDescription.ConverterType, converterTypeDescription.ConverterIsPartial)
                        : new JsonConverterType.None()));
            }
        }

        return mappings;
    }
}
