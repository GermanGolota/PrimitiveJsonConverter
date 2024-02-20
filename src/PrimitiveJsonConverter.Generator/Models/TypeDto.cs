using Microsoft.CodeAnalysis;

namespace PrimitiveJsonConverter.Generator;

internal sealed record TypeDto(
    string Name,
    TypeDeclarationKind Kind,
    Namespace ContainingNamespace,
    Accessibility Accessibility
    );
