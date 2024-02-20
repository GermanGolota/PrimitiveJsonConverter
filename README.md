This package source generates System.Text.Json.JsonConverter 
that converts a Value Object into its primitive form 
using implicit/explicit operators.

The following class would be serialized/deserialized as an integer value.
```csharp
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
}
```

Currently supported types:
- `string`
- `Guid`
- `short`, `int`, `long`
- `ushort`, `uint`, `ulong`
- `double`, `float`, `decimal`
- `DateTime`, `DateTimeOffset`
- `bool`
- `byte`
