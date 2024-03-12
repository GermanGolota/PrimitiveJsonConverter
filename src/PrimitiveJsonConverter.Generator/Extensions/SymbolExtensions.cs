using Microsoft.CodeAnalysis;

namespace PrimitiveJsonConverter.Generator;

internal static class SymbolExtensions
{
    public static string GetName(this ITypeSymbol type) =>
       (type.IsValueType, type.NullableAnnotation) switch
       {
           (true, NullableAnnotation.Annotated) =>
               type is INamedTypeSymbol t
               && t.IsGenericType
               // System.Nullable`1
               && t.TypeArguments.Length > 0
                   ? t.TypeArguments[0].Name
                   : type.Name,
           _ => type.Name
       };

}
