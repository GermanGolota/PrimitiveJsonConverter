using PrimitiveJsonConverter;

namespace PrimitiveJsonConverter.Generator.Sample;

[JsonPrimitive]
public sealed partial record FileName
{
    public FileName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public static implicit operator FileName?(string? value) => value is null ? null : new(value);
    public static implicit operator string?(FileName? value) => value?.Value;
}