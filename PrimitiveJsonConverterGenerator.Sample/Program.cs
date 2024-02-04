using System.Text.Json;

var value = new DiceRoll(32);
string jsonString = JsonSerializer.Serialize(value);
Console.WriteLine(jsonString);