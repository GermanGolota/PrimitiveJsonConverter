using System.Text.Json.Serialization;

namespace PrimitiveJsonConverter.Generator.Sample;

[JsonPrimitiveConverter(typeof(EnvironmentName))]
internal sealed partial class EnvironmentNameConverter { }

[JsonConverter(typeof(EnvironmentNameConverter))]
public sealed record EnvironmentName
{
    public EnvironmentName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public static implicit operator EnvironmentName(string value) => new(value);
    public static implicit operator string(EnvironmentName value) => value.Value;
}