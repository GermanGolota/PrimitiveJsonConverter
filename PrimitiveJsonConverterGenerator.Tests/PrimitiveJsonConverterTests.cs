namespace PrimitiveJsonConverterGenerator.Tests;

public class PrimitiveJsonConverterTests
{
    [Fact]
    public Task GeneratesEnumExtensionsCorrectly()
    {
        // The source code to test
        var source = """
using PrimitiveJsonConverterGenerator;
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonConverter(typeof(PrimitiveJsonConverterFactory))]
public sealed record DiceRoll
{
    public DiceRoll(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Value = value;
    }

    public int Value { get; }

    public static implicit operator DiceRoll(int value)
    {
        return new(value);
    }

    public static implicit operator int(DiceRoll value)
    {
        return value.Value;
    }
}
""";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }
}