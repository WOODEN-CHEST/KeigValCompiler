namespace KeigValCompiler.Semantician.Types;

/* A type as the compiler reasons about it, as opposed to how source code writes it: a declared type with
 * the types given for its generic parameters, or a generic parameter itself. Arrays and nullable values
 * are the library types they stand for, so "int[]" is Array<int> and "int?" is Nullable<int>.
 *
 * A '?' on a reference type only annotates it. The annotation is kept, so that nullability can be
 * checked, but as in C# it plays no part in whether two types are the same: Equals and GetHashCode leave
 * it out, and "string" and "string?" are one type. */
internal abstract class SemanticType : IEquatable<SemanticType>
{
    // Internal fields.
    /* Whether a '?' annotates this type. Never set on a value type, whose '?' makes it a Nullable. */
    internal bool IsNullableAnnotated { get; private init; }

    /* A structure or an enum, or a generic parameter constrained to be one. */
    internal abstract bool IsValueType { get; }


    // Constructors.
    internal SemanticType(bool isNullableAnnotated)
    {
        IsNullableAnnotated = isNullableAnnotated;
    }


    // Internal methods.
    /* This type with each generic parameter the substitution has a type for replaced by that type. */
    internal abstract SemanticType Substitute(TypeSubstitution substitution);

    internal abstract SemanticType WithNullableAnnotation(bool isNullableAnnotated);


    // Inherited methods.
    public abstract bool Equals(SemanticType? other);

    public override bool Equals(object? obj)
    {
        return Equals(obj as SemanticType);
    }

    public abstract override int GetHashCode();
}
