namespace PrimitiveJsonConverter.Generator;

internal sealed class ConverterTypeDescriptionComparer : IEqualityComparer<ConverterTypeDescription>
{
    private static TypeDescriptionComparer _typeDescriptionComparer = new();

    public bool Equals(ConverterTypeDescription x, ConverterTypeDescription y) =>
        _typeDescriptionComparer.Equals(x.ClassType, y.ClassType)
        && x.ConverterType == y.ConverterType
        && x.ConverterIsPartial == y.ConverterIsPartial
        && x.ConverterLocation == y.ConverterLocation;

    public int GetHashCode(ConverterTypeDescription obj) =>
        obj.GetHashCode();
}