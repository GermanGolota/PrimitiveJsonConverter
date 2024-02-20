﻿using System.CodeDom.Compiler;
using System.Reflection;

namespace PrimitiveJsonConverter.Generator;

internal static class IndentedTextWriterExtensions
{
    public static void StartNamespace(this IndentedTextWriter source, Namespace @namespace)
    {
        if (@namespace is Namespace.Local local)
        {
            source.Write("namespace ");
            source.WriteLine(local.Value);
            source.WriteLine("{");
            source.Indent++;
        }
    }

    public static void CloseNamespace(this IndentedTextWriter source, Namespace @namespace)
    {
        if (@namespace is Namespace.Local)
        {
            source.Indent--;
            source.WriteLine("}");
        }
    }

    public static void WriteAutoGeneratedCommentLine(this IndentedTextWriter writer) =>
        writer.WriteLine("// <auto-generated/>");

    public static void WriteGeneratedCodeAttributeLine(this IndentedTextWriter writer) =>
        writer.WriteLine($"""[global::System.CodeDom.Compiler.GeneratedCodeAttribute("PrimitiveJsonConverter.Generator", "{GeneratorVersion}")]""");

    public static string? GeneratorVersion { get; } = GetGeneratorVersion();

    private static string? GetGeneratorVersion()
    {
        var assembly = typeof(SerializerWriter).Assembly;
        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attribute?.InformationalVersion;
    }

    public static void WritePragmaWarningDisableLine(this IndentedTextWriter writer)
    {
        writer.WriteLine("// Disable CS1591 - Missing XML comment on public member");
        writer.WriteLine("#pragma warning disable 1591");
        writer.WriteLine("// Disable CS8604 - Possible null reference argument for parameter.");
        writer.WriteLine("#pragma warning disable 8604");
    }

    public static void WritePragmaWarningRestoreLine(this IndentedTextWriter writer)
    {
        writer.WriteLine("// Disable CS1591 - Missing XML comment on public member");
        writer.WriteLine("#pragma warning restore 1591");
        writer.WriteLine("// Disable CS8604 - Possible null reference argument for parameter.");
        writer.WriteLine("#pragma warning restore 8604");
    }
}
