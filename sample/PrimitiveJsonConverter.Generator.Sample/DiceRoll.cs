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

    public static implicit operator DiceRoll(int value) => new(value);
    public static implicit operator int(DiceRoll value) => value.Value;
    public static implicit operator DiceRoll(string value) => new(int.Parse(value));
}
