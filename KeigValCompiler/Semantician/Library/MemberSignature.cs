using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Library;

/* Everything which tells one member of a library type from another: the type declaring it, its kind,
 * its name or operator, whether it is static, its type, and its parameters. Two signatures are equal
 * when all of those are, which is how a builtin member in the library is matched to what the compiler
 * implements for it.
 *
 * The type is the return type of a method or an operator, the target of a conversion, and the type of
 * a property or an indexer, for the setter as well as the getter. A constructor has none. Explicit
 * interface implementations are not told apart yet, since no builtin member is one. */
internal sealed class MemberSignature : IEquatable<MemberSignature>
{
    // Internal fields.
    internal required LibraryType DeclaringType { get; init; }
    internal required MemberSignatureKind Kind { get; init; }

    /* The name of a method or a property, and null for every other kind. */
    internal string? Name { get; init; } = null;

    /* The operator of an operator, and null for every other kind. */
    internal OverloadableOperator? Operator { get; init; } = null;

    internal bool IsStatic { get; init; } = false;

    /* Null for a void method and for a constructor. */
    internal SignatureType? Type { get; init; } = null;

    internal IReadOnlyList<SignatureParameter> Parameters { get; init; } = Array.Empty<SignatureParameter>();
    internal int GenericParameterCount { get; init; } = 0;


    // Inherited methods.
    public bool Equals(MemberSignature? other)
    {
        return (other != null)
            && (DeclaringType == other.DeclaringType)
            && (Kind == other.Kind)
            && (Name == other.Name)
            && (Operator == other.Operator)
            && (IsStatic == other.IsStatic)
            && Equals(Type, other.Type)
            && Parameters.SequenceEqual(other.Parameters)
            && (GenericParameterCount == other.GenericParameterCount);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as MemberSignature);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DeclaringType, Kind, Name, Operator, IsStatic, Parameters.Count);
    }

    public override string ToString()
    {
        return SignatureFormatter.Format(this);
    }
}
