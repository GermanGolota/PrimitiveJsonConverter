using System.Text.Json.Serialization;
using PrimitiveJsonConverterGenerator;

namespace PrimitiveJsonConverterGenerator.Sample;

[JsonConverter(typeof(PrimitiveJsonConverterFactory))]
public sealed record FileName
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

    public static implicit operator FileName(string value) => new(value);
    public static implicit operator string(FileName value) => value.Value;
}