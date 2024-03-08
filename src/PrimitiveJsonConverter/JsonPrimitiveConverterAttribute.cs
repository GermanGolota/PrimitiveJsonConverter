namespace PrimitiveJsonConverter;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class JsonPrimitiveConverterAttribute : Attribute
{
    public JsonPrimitiveConverterAttribute(Type classType)
    {
        ClassType = classType;
    }

    public Type ClassType { get; }
}
