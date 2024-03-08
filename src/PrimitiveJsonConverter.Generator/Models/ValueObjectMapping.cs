namespace PrimitiveJsonConverter.Generator;

internal sealed record ValueObjectMapping(string PrimitiveType, TypeDto ClassType, JsonConverterType ConverterType);

internal abstract record JsonConverterType
{
    public sealed record None() : JsonConverterType;
    public sealed record Some(TypeDto Type, bool IsPartial) : JsonConverterType;
}
