using PrimitiveJsonConverter.Generator.Sample;
using System.Text.Json;

var value = new DiceRoll(32);
var jsonString = JsonSerializer.Serialize(value);
Console.WriteLine(jsonString);
var valueDeserialized = JsonSerializer.Deserialize<DiceRoll>(jsonString)!;
Console.WriteLine(valueDeserialized.Value);

var envName = new EnvironmentName("dev");
var envJsonString = JsonSerializer.Serialize(envName);
Console.WriteLine(envJsonString);
var envValueDeserialized = JsonSerializer.Deserialize<EnvironmentName>(envJsonString)!;
Console.WriteLine(envValueDeserialized.Value);