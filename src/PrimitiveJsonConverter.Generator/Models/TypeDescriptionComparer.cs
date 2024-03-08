namespace PrimitiveJsonConverter.Generator;

internal sealed class TypeDescriptionComparer : IEqualityComparer<TypeDescription>
{
    public bool Equals(TypeDescription x, TypeDescription y) =>
        x.Type == y.Type
        && x.IsPartial == y.IsPartial
        && x.Location == y.Location
        && x.TypeMaps.SequenceEqual(y.TypeMaps);
    
    public int GetHashCode(TypeDescription obj) =>
        obj.GetHashCode();
}
