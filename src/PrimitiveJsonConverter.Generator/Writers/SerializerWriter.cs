﻿using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;

namespace PrimitiveJsonConverter.Generator;

internal static class SerializerWriter
{
    private sealed record SerializationMethods(string[] TokenTypes, string ReadMethod, string WriteMethod, bool IsValue);

    private static Dictionary<string, SerializationMethods> _typeToMethods = new Dictionary<string, SerializationMethods>()
    {
        { "global::System.Int16", new(["Number"], "GetInt16", "WriteNumberValue", true)},
        { "global::System.Int32", new(["Number"], "GetInt32", "WriteNumberValue", true)},
        { "global::System.Int64", new(["Number"], "GetInt64", "WriteNumberValue", true)},

        { "global::System.UInt16", new(["Number"], "GetUInt16", "WriteNumberValue", true)},
        { "global::System.UInt32", new(["Number"], "GetUInt32", "WriteNumberValue", true)},
        { "global::System.UInt64", new(["Number"], "GetUInt64", "WriteNumberValue", true)},

        { "global::System.Single", new(["Number"], "GetSingle", "WriteNumberValue", true)},
        { "global::System.Double", new(["Number"], "GetDouble", "WriteNumberValue", true)},
        { "global::System.Decimal", new(["Number"], "GetDecimal", "WriteNumberValue", true)},

        { "global::System.String", new(["String"], "GetString", "WriteStringValue", false)},
        { "global::System.Guid", new(["String"], "GetGuid", "WriteStringValue", true)},

        { "global::System.Boolean", new(["True", "False"], "GetBoolean", "WriteBooleanValue", true)},

        { "global::System.Byte", new(["Number"], "GetByte", "WriteNumberValue", true)},

        { "global::System.DateTime", new(["String"], "GetDateTime", "WriteStringValue", true)},
        { "global::System.DateTimeOffset", new(["String"], "GetDateTimeOffset", "WriteStringValue", true)},
    };

    public static (string Code, string ClassName) WriteCode(ValueObjectMapping mapping)
    {
        var methods = _typeToMethods[mapping.PrimitiveType];

        string className = GetClassName(mapping);

        var mappingTypeName = mapping.ClassType.ContainingNamespace.Format(mapping.ClassType.Name);

        var codeBuilder = new StringBuilder();
        using var writer = new StringWriter(codeBuilder, CultureInfo.InvariantCulture);
        using var source = new IndentedTextWriter(writer, "\t");

        source.WriteAutoGeneratedCommentLine();
        source.WritePragmaWarningDisableLine();
        source.WriteLine("﻿using System.Text.Json;");
        source.WriteLine("﻿#nullable enable");
        if (mapping.ConverterType is JsonConverterType.Some s)
        {
            source.StartNamespace(s.Type.ContainingNamespace);
        }
        else
        {
            source.StartNamespace(mapping.ClassType.ContainingNamespace);
        }
        source.WriteGeneratedCodeAttributeLine();
        if (mapping.ConverterType is JsonConverterType.Some s2)
        {
            var type = s2.Type;
            source.WriteLine($"{type.Accessibility.ToKeyword()} partial {type.Kind.ToString().ToLower()} {type.Name} : global::System.Text.Json.Serialization.JsonConverter<{mappingTypeName}>");
        }
        else
        {
            source.WriteLine($"public partial class {className} : global::System.Text.Json.Serialization.JsonConverter<{mappingTypeName}>");
        }
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine("public override global::System.Boolean CanConvert(global::System.Type typeToConvert)");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"return typeToConvert == typeof({mappingTypeName});");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine($"public override {mappingTypeName}? Read(ref Utf8JsonReader reader, global::System.Type typeToConvert, JsonSerializerOptions options)");
        source.WriteLine("{");
        source.Indent++;
        source.Write("if (");
        for (var i = 0; i < methods.TokenTypes.Length; i++)
        {
            var tokenType = methods.TokenTypes[i];
            source.Write($"reader.TokenType == JsonTokenType.{tokenType}");
            if (i != methods.TokenTypes.Length - 1)
            {
                source.Write(" || ");
            }
        }
        source.WriteLine(")");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"return ({mappingTypeName}?) reader.{methods.ReadMethod}();");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine();
        source.WriteLine($"return null;");
        source.Indent--;
        source.WriteLine("}");
        source.WriteLine($"public override void Write(Utf8JsonWriter writer, {mappingTypeName} value, JsonSerializerOptions options)");
        source.WriteLine("{");
        source.Indent++;
        source.WriteLine($"{mapping.PrimitiveType}? temp = ({mapping.PrimitiveType}?)value;");
        var nullCheck = methods.IsValue ? ".HasValue" : " is not null";
        source.WriteLine($"if (temp{nullCheck})");
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

        if (mapping.ClassType.ContainingNamespace is Namespace.Local)
        {
            source.Indent--;
            source.WriteLine("}");
        }

        source.WriteLine("﻿#nullable disable");
        source.WritePragmaWarningDisableLine();

        return (codeBuilder.ToString(), className);
    }

    private static string GetClassName(ValueObjectMapping mapping)
    {
        return mapping.ConverterType is JsonConverterType.Some converter
            ? converter.Type.Name
            : $"{mapping.ClassType.Name}PrimitiveJsonConverter";
    }
}
