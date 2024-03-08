using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace PrimitiveJsonConverter.Generator;

internal sealed record TypeDto(
    string Name,
    TypeDeclarationKind Kind,
    Namespace ContainingNamespace,
    Accessibility Accessibility
);

internal sealed record ConverterTypeDescription(
    TypeDescription ClassType, 
    TypeDto ConverterType, 
    bool ConverterIsPartial,
    Location ConverterLocation
    );

internal sealed record TypeDescription(
    TypeDto Type,
    bool IsPartial,
    Location Location,
    ImmutableArray<TypeMap> TypeMaps
    );

internal sealed record TypeMap(string InType, string OutType);
