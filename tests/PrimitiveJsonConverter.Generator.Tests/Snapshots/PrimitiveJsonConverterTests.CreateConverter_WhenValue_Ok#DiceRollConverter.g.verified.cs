﻿//HintName: DiceRollConverter.g.cs
// <auto-generated/>
// Disable CS1591 - Missing XML comment on public member
#pragma warning disable 1591
// Disable CS8604 - Possible null reference argument for parameter.
#pragma warning disable 8604
﻿#nullable enable
[global::System.CodeDom.Compiler.GeneratedCodeAttribute("PrimitiveJsonConverter.Generator", "1.0.9")]
internal partial class DiceRollConverter : global::System.Text.Json.Serialization.JsonConverter<global::DiceRoll>
{
	public override global::System.Boolean CanConvert(global::System.Type typeToConvert)
	{
		return typeToConvert == typeof(global::DiceRoll);
	}
	public override global::DiceRoll? Read(ref System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
	{
		if (reader.TokenType == System.Text.Json.JsonTokenType.Number)
		{
			return (global::DiceRoll?) reader.GetInt32();
		}
		
		return null;
	}
	public override void Write(System.Text.Json.Utf8JsonWriter writer, global::DiceRoll value, System.Text.Json.JsonSerializerOptions options)
	{
		global::System.Int32? temp = (global::System.Int32?)value;
		if (temp.HasValue)
		{
			writer.WriteNumberValue(temp.Value);
		}
		else
		{
			writer.WriteNullValue();
		}
	}
}
﻿#nullable disable
// Disable CS1591 - Missing XML comment on public member
#pragma warning disable 1591
// Disable CS8604 - Possible null reference argument for parameter.
#pragma warning disable 8604
