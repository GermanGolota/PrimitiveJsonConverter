using Microsoft.CodeAnalysis;

namespace PrimitiveJsonConverterGenerator;

internal sealed record TypeDto(
    string Name,
    TypeDeclarationKind Kind,
    Namespace ContainingNamespace,
    Accessibility Accessibility
    );
