using Microsoft.CodeAnalysis;

namespace PrimitiveJsonConverterGenerator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor NoConversionOperators = new DiagnosticDescriptor(id: "PRIM001",
                                                                                              title: "Not enough conversion operators",
                                                                                              messageFormat: "Type '{0}' must contain a pair of conversion operators to use 'JsonPrimitiveAttribute'",
                                                                                              category: "PrimitiveGen",
                                                                                              DiagnosticSeverity.Warning,
                                                                                              isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TooManyConversionOperators = new DiagnosticDescriptor(id: "PRIM002",
                                                                                             title: "Too many conversion operators",
                                                                                             messageFormat: "Type '{0}' must contain only one pair of conversion operators to use 'JsonPrimitiveAttribute'",
                                                                                             category: "PrimitiveGen",
                                                                                             DiagnosticSeverity.Warning,
                                                                                             isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotPartial = new DiagnosticDescriptor(id: "PRIM003",
                                                                                      title: "JsonPrimitive type is not partial",
                                                                                      messageFormat: "Type '{0}' must be partial to use 'JsonPrimitiveAttribute'",
                                                                                      category: "PrimitiveGen",
                                                                                      DiagnosticSeverity.Warning,
                                                                                      isEnabledByDefault: true);
}
