﻿//HintName: FileNamePrimitiveJsonConverter.g.cs
// <auto-generated/>
// Disable CS1591 - Missing XML comment on public member
#pragma warning disable 1591
// Disable CS8604 - Possible null reference argument for parameter.
#pragma warning disable 8604
﻿using System.Text.Json;
﻿#nullable enable
[global::System.CodeDom.Compiler.GeneratedCodeAttribute("PrimitiveJsonConverter.Generator", "1.0.8")]
public partial class FileNamePrimitiveJsonConverter : global::System.Text.Json.Serialization.JsonConverter<global::FileName>
{
	public override global::System.Boolean CanConvert(global::System.Type typeToConvert)
	{
		return typeToConvert == typeof(global::FileName);
	}
	public override global::FileName? Read(ref Utf8JsonReader reader, global::System.Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			return (global::FileName?) reader.GetString();
		}
		
		return null;
	}
	public override void Write(Utf8JsonWriter writer, global::FileName value, JsonSerializerOptions options)
	{
		global::System.String? temp = (global::System.String?)value;
		if (temp is not null)
		{
			writer.WriteStringValue(temp!);
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
