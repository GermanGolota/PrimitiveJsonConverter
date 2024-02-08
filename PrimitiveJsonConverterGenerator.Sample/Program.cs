using System.Text.Json;

var value = new DiceRoll(32);
var jsonString = JsonSerializer.Serialize(value);
Console.WriteLine(jsonString);
var valueDeserialized = JsonSerializer.Deserialize<DiceRoll>(jsonString)!;
Console.WriteLine(valueDeserialized.Value);