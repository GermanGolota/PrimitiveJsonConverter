using Microsoft.CodeAnalysis;

namespace PrimitiveJsonConverter.Generator;

internal abstract record Namespace
{
    public abstract string Format(string typeName);

    public sealed record Global : Namespace
    {
        public override string Format(string typeName) =>
            $"global::{typeName}";
    }

    public sealed record Local(string Value) : Namespace
    {
        public override string Format(string typeName) =>
            $"global::{Value}.{typeName}";
    }

    public static Namespace From(INamespaceSymbol symbol) =>
        symbol.IsGlobalNamespace
        ? new Global()
        : new Local(symbol.ToString());
}
