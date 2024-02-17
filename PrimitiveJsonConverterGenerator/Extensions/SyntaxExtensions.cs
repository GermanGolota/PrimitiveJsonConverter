using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PrimitiveJsonConverterGenerator;

internal static class SyntaxExtensions
{
    public static TypeDeclarationKind GetTypeKind(this TypeDeclarationSyntax syntax) =>
        syntax switch
        {
            RecordDeclarationSyntax => TypeDeclarationKind.Record,
            StructDeclarationSyntax => TypeDeclarationKind.Struct,
            ClassDeclarationSyntax => TypeDeclarationKind.Class,
            _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
        };

    public static string ToKeyword(this Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.ProtectedOrInternal
            or Accessibility.ProtectedOrFriend
                => "protected internal",
            Accessibility.Internal or Accessibility.Friend => "internal",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedAndInternal
            or Accessibility.ProtectedAndFriend
                => "private protected",
            Accessibility.Private => "private",
            Accessibility.NotApplicable => "",
            _ => "",
        };

}
