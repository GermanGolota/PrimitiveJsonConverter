using System.Text.Json.Serialization;

[JsonConverter(typeof(PrimitiveJsonConverterGenerator.PrimitiveJsonConverterFactory))]
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
    public static implicit operator DiceRoll(string value) => new(int.Parse(value));
    /*
        public static explicit operator int(DiceRoll value) => value.Value;
        public static explicit operator string(DiceRoll value) => value.Value.ToString();*/
}

namespace Test.Test2
{
    [JsonConverter(typeof(PrimitiveJsonConverterGenerator.PrimitiveJsonConverterFactory))]
    public sealed record DiceRoll2
    {
        public DiceRoll2(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Value = value;
        }

        public int Value { get; }

        public static implicit operator DiceRoll2(int value) => new(value);
        public static implicit operator int(DiceRoll2 value) => value.Value;
        public static implicit operator DiceRoll2(string value) => new(int.Parse(value));
        /*
            public static explicit operator int(DiceRoll value) => value.Value;
            public static explicit operator string(DiceRoll value) => value.Value.ToString();*/
    }

}