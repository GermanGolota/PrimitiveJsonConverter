﻿using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;

namespace PrimitiveJsonConverterGenerator;

internal static class SerializerWriter
{
    private sealed record SerializationMethods(string TokenType, string ReadMethod, string WriteMethod, bool IsValue);

    private static Dictionary<string, SerializationMethods> _typeToMethods = new Dictionary<string, SerializationMethods>()
    {
        { "global::System.Int16", new("Number", "GetInt16", "WriteNumberValue", true)},
        { "global::System.Int32", new("Number", "GetInt32", "WriteNumberValue", true)},
        { "global::System.Int64", new("Number", "GetInt64", "WriteNumberValue", true)},

        { "global::System.UInt16", new("Number", "GetUInt16", "WriteNumberValue", true)},
        { "global::System.UInt32", new("Number", "GetUInt32", "WriteNumberValue", true)},
        { "global::System.UInt64", new("Number", "GetUInt64", "WriteNumberValue", true)},

        { "global::System.Single", new("Number", "GetSingle", "WriteNumberValue", true)},
        { "global::System.Double", new("Number", "GetDouble", "WriteNumberValue", true)},

        { "global::System.String", new("String", "GetString", "WriteStringValue", false)},
        { "global::System.Guid", new("String", "GetGuid", "WriteStringValue", true)},
    };

    public static (string Code, string ClassName) WriteCode(VoMapping mapping)
    {
        var methods = _typeToMethods[mapping.PrimitiveType];

        var className = $"{mapping.ClassName}PrimitiveJsonConverter";

        var codeBuilder = new StringBuilder();
        using var writer = new StringWriter(codeBuilder, CultureInfo.InvariantCulture);
        using var source = new IndentedTextWriter(writer, "\t");

        source.WriteLine("﻿// <auto-generated />");
        source.WriteLine("﻿using System.Text.Json;");
        source.WriteLine("﻿#nullable enable");
        source.WriteLine($"namespace {nameof(PrimitiveJsonConverterGenerator)}");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"internal sealed class {className} : global::System.Text.Json.Serialization.JsonConverter<{mapping.ClassType}>");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"public static void Load()");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"global::PrimitiveJsonConverterGenerator.PrimitiveJsonConverterFactory.Converters.Add(typeof({mapping.ClassType}), new {className}());");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine("public override global::System.Boolean CanConvert(global::System.Type typeToConvert)");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"return typeToConvert == typeof({mapping.ClassType});");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine($"public override {mapping.ClassType}? Read(ref Utf8JsonReader reader, global::System.Type typeToConvert, JsonSerializerOptions options)");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine("reader.Read();");
        source.WriteLine($"if (reader.TokenType == JsonTokenType.{methods.TokenType}) return ({mapping.ClassType}?) reader.{methods.ReadMethod}();");
        source.WriteLine($"return null;");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine($"public override void Write(Utf8JsonWriter writer, {mapping.ClassType} value, JsonSerializerOptions options)");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"{mapping.PrimitiveType}? temp = ({mapping.PrimitiveType}?)value;");
        var nullCheck = methods.IsValue ? ".HasValue" : " is not null";
        source.WriteLine($"if(temp{nullCheck})");
        source.WriteLine("{");
        source.Indent++;
        var valueGetter = methods.IsValue ? ".Value" : "!";
        source.WriteLine($"writer.{methods.WriteMethod}(temp{valueGetter});");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine("else");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine("writer.WriteNullValue();");
        source.Indent--;
        source.WriteLine("}");
        source.Indent--;
        source.WriteLine("}");
        source.Indent--;
        source.WriteLine("}");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine("﻿#nullable disable");

        return (codeBuilder.ToString(), className);
    }
}
