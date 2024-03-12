namespace PrimitiveJsonConverter.Generator.Tests;

public class PrimitiveJsonConverterTests
{
    [Fact]
    public Task CreateClass_WhenValue_Ok()
    {
        // The source code to test
        var source = """
using PrimitiveJsonConverter;

[JsonPrimitive]
public sealed partial record DiceRoll
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

    [Fact]
    public Task CreateClass_WhenNullableValue_Ok()
    {
        // The source code to test
        var source = """
using PrimitiveJsonConverter;

[JsonPrimitive]
public sealed partial record DiceRoll
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

    public static implicit operator DiceRoll?(int? value)
    {
        if(value.HasValue)
        {
            return new(value.Value);
        }
        return null;
    }

    public static implicit operator int?(DiceRoll? value)
    {
        return value?.Value;
    }
}
""";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CreateConverter_WhenValue_Ok()
    {
        // The source code to test
        var source = """
using PrimitiveJsonConverter;

[JsonPrimitiveConverter(typeof(DiceRoll))]
internal sealed partial class DiceRollConverter {}

[JsonConverter(typeof(DiceRollConverter))]
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

    public static implicit operator DiceRoll(int value) => new(value);
    public static implicit operator int(DiceRoll value) => value.Value;
}
""";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }
    
    [Fact]
    public Task CreateClass_WhenReference_Ok()
    {
        // The source code to test
        var source = """
using PrimitiveJsonConverter;

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
""";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

}