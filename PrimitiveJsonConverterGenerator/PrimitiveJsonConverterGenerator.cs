using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
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
            transform: static (n, _) => n.Node as TypeDeclarationSyntax
            )
            .Where(_ => _ is not null);

        var compilation = context.CompilationProvider.Combine(provider.Collect());

        context.RegisterSourceOutput(compilation, (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private static readonly DiagnosticDescriptor NoConversionOperators = new DiagnosticDescriptor(id: "PRIM001",
                                                                                              title: "Not enough conversion operators",
                                                                                              messageFormat: $"Type '{{0}}' must contain a pair of conversion operators to use '{ConstantCode.FactoryName}'",
                                                                                              category: "PrimitiveGen",
                                                                                              DiagnosticSeverity.Warning,
                                                                                              isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor TooManyConversionOperators = new DiagnosticDescriptor(id: "PRIM002",
                                                                                             title: "Too many conversion operators",
                                                                                             messageFormat: $"Type '{{0}}' must contain only one pair of conversion operators to use '{ConstantCode.FactoryName}'",
                                                                                             category: "PrimitiveGen",
                                                                                             DiagnosticSeverity.Warning,
                                                                                             isEnabledByDefault: true);

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

        var diagnostics = new List<Diagnostic>();
        var voMappings = new List<VoMapping>();

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
            var mappings = new List<VoMapping>();
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
                    0 => NoConversionOperators,
                    _ => TooManyConversionOperators
                };
                diagnostics.Add(Diagnostic.Create(desc, symbol.Locations[0], new[] { symbolType }));
            }
        }

        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        var classes = new List<string>();
        foreach (var mapping in voMappings)
        {
            var (code, className) = BuildCode(mapping);
            context.AddSource($"{className}.g.cs", code);
            classes.Add(className);
        }

        var loaderCode = BuildLoader(classes);
        context.AddSource($"{ConstantCode.LoaderName}2.g.cs", loaderCode);
    }

    private string BuildLoader(List<string> classes)
    {
        var codeBuilder = new StringBuilder();
        using var writer = new StringWriter(codeBuilder, CultureInfo.InvariantCulture);
        using var source = new IndentedTextWriter(writer, "\t");
        source.WriteLine("﻿// <auto-generated />");
        source.WriteLine("﻿#nullable enable");
        source.WriteLine("﻿using System.Text.Json;");
        source.WriteLine($"namespace {nameof(PrimitiveJsonConverterGenerator)}");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"internal static partial class {ConstantCode.LoaderName}");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"public static partial void Load()");
        source.WriteLine("{");
        source.Indent++;
        foreach (var @class in classes)
        {
            source.WriteLine($"{@class}.Load();");
        }
        source.Indent--;
        source.WriteLine("}");
        source.Indent--;
        source.WriteLine("}");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine("﻿#nullable disable");
        return codeBuilder.ToString();
    }

    private sealed record Method(string TokenType, string ReadMethod, string WriteMethod, bool IsValue);

    private static Dictionary<string, Method> _typeToMethods = new Dictionary<string, Method>()
    {
        { "global::System.Int16", new("Number", "GetInt16", "WriteNumberValue", true)},
        { "global::System.Int32", new("Number", "GetInt32", "WriteNumberValue", true)},
        { "global::System.Int64", new("Number", "GetInt64", "WriteNumberValue", true)},

        { "global::System.UInt16", new("Number", "GetUInt16", "WriteNumberValue", true)},
        { "global::System.UInt32", new("Number", "GetUInt32", "WriteNumberValue", true)},
        { "global::System.UInt64", new("Number", "GetUInt64", "WriteNumberValue", true)},

        { "global::System.Single", new("Number", "GetSingle", "WriteNumberValue", true)},
        { "global::System.Double", new("Number", "GetDouble", "WriteNumberValue", true)},

        { "global::System.String", new("String", "GetString", "WriteStringValue", false)},
        { "global::System.Guid", new("String", "GetGuid", "WriteStringValue", true)},
    };

    private (string Code, string ClassName) BuildCode(VoMapping mapping)
    {
        var methods = _typeToMethods[mapping.PrimitiveType];

        var className = $"{mapping.ClassName}PrimitiveJsonConverter";

        var codeBuilder = new StringBuilder();
        using var writer = new StringWriter(codeBuilder, CultureInfo.InvariantCulture);
        using var source = new IndentedTextWriter(writer, "\t");

        source.WriteLine("﻿// <auto-generated />");
        source.WriteLine("﻿using System.Text.Json;");
        source.WriteLine("﻿#nullable enable");
        source.WriteLine($"namespace {nameof(PrimitiveJsonConverterGenerator)}");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"internal sealed class {className} : global::System.Text.Json.Serialization.JsonConverter<{mapping.ClassType}>");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"public static void Load()");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"global::PrimitiveJsonConverterGenerator.PrimitiveJsonConverterFactory.Converters.Add(typeof({mapping.ClassType}), new {className}());");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine("public override global::System.Boolean CanConvert(global::System.Type typeToConvert)");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"return typeToConvert == typeof({mapping.ClassType});");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine($"public override {mapping.ClassType}? Read(ref Utf8JsonReader reader, global::System.Type typeToConvert, JsonSerializerOptions options)");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine("reader.Read();");
        source.WriteLine($"if (reader.TokenType == JsonTokenType.{methods.TokenType}) return ({mapping.ClassType}?) reader.{methods.ReadMethod}();");
        source.WriteLine($"return null;");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine($"public override void Write(Utf8JsonWriter writer, {mapping.ClassType} value, JsonSerializerOptions options)");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"{mapping.PrimitiveType}? temp = ({mapping.PrimitiveType}?)value;");
        var nullCheck = methods.IsValue ? ".HasValue" : " is not null";
        source.WriteLine($"if(temp{nullCheck})");
        source.WriteLine("{");
        source.Indent++;
        var valueGetter = methods.IsValue ? ".Value" : "!";
        source.WriteLine($"writer.{methods.WriteMethod}(temp{valueGetter});");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine("else");
        source.WriteLine("{");
        source.Indent++; 
        source.WriteLine("writer.WriteNullValue();");
        source.Indent--;
        source.WriteLine("}");
        source.Indent--;
        source.WriteLine("}");
        source.Indent--;
        source.WriteLine("}");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine("﻿#nullable disable");

        return (codeBuilder.ToString(), className);
    }

    private static string ToGlobal(string @namespace, string typeName) =>
        @namespace switch
        {
            "<global namespace>" => $"global::{typeName}",
            _ => $"global::{@namespace}.{typeName}"
        };

    private sealed record TypeMap(string InType, string OutType);
    private sealed record VoMapping(string PrimitiveType, string ClassType, string ClassName);

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
}
