﻿using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;

namespace PrimitiveJsonConverter.Generator;

internal static class VoWriter
{
    public static (string SourceCode, object ClassName) WriteCode(TypeDto type, string converterClassName)
    {
        var codeBuilder = new StringBuilder();
        using var writer = new StringWriter(codeBuilder, CultureInfo.InvariantCulture);
        using var source = new IndentedTextWriter(writer, "\t");

        source.WriteAutoGeneratedCommentLine();
        source.WritePragmaWarningDisableLine();

        source.StartNamespace(type.ContainingNamespace);

        source.WriteGeneratedCodeAttributeLine();
        source.WriteLine($"[global::System.Text.Json.Serialization.JsonConverter(typeof({converterClassName}))]");
        source.WriteLine($"{type.Accessibility.ToKeyword()} partial {type.Kind.ToString().ToLower()} {type.Name}");

        source.WriteLine("{");
        source.WriteLine("}");

        source.CloseNamespace(type.ContainingNamespace);

        source.WritePragmaWarningRestoreLine();

        return (codeBuilder.ToString(), type.Name);
    }
}
