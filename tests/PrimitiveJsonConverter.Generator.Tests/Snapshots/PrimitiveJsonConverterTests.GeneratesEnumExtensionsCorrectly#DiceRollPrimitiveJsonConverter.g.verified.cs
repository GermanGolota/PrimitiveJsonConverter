﻿//HintName: DiceRollPrimitiveJsonConverter.g.cs
// <auto-generated/>
// Disable CS1591 - Missing XML comment on public member
#pragma warning disable 1591
// Disable CS8604 - Possible null reference argument for parameter.
#pragma warning disable 8604
﻿using System.Text.Json;
﻿#nullable enable
[global::System.CodeDom.Compiler.GeneratedCodeAttribute("PrimitiveJsonConverter.Generator", "1.0.4+649e6fb2549fc496aeddb622b963e3d01ee0cc2a")]
public sealed class DiceRollPrimitiveJsonConverter : global::System.Text.Json.Serialization.JsonConverter<global::DiceRoll>
{
	public override global::System.Boolean CanConvert(global::System.Type typeToConvert)
	{
		return typeToConvert == typeof(global::DiceRoll);
	}
	public override global::DiceRoll? Read(ref Utf8JsonReader reader, global::System.Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Number)
		{
			return (global::DiceRoll?) reader.GetInt32();
		}
		
		return null;
	}
	public override void Write(Utf8JsonWriter writer, global::DiceRoll value, JsonSerializerOptions options)
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
