using Microsoft.CodeAnalysis;

namespace PrimitiveJsonConverterGenerator;

internal sealed record TypeDto(
    string Name,
    TypeKind Kind,
    Namespace ContainingNamespace,
    Accessibility Accessibility
    );
