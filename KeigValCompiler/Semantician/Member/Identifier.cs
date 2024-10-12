﻿namespace KeigValCompiler.Semantician.Member;

internal sealed class Identifier
{
    // Fields.
    internal string SelfName { get; set; }
    internal string SourceCodeName { get; private init; }
    internal string? ResolvedName { get; set; }
    internal IIdentifiable? Target { get; set; }


    // Constructors.
    internal Identifier(string sourceCodeName)
    {
        SourceCodeName = sourceCodeName ?? throw new ArgumentNullException(nameof(sourceCodeName));
    }


    // Methods.


    // Inherited methods.
    public override bool Equals(object? obj)
    {
        if (obj is Identifier TargetIdentifier)
        {
            return ResolvedName == TargetIdentifier.ResolvedName;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return ResolvedName?.GetHashCode() ?? 0;
    }

    public override string ToString()
    {
        return ResolvedName ?? "Unresolved Identifier";
    }
}