using static System.Console;
using PrimitiveJsonConverterGenerator;
using System.Text.Json;

var a = 32;
var value = new DiceRoll(32);



string jsonString = JsonSerializer.Serialize(value);

var b = 32;
