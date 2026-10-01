namespace KeigValCompiler.Semantician.Types;

/* The type of a value which could not be bound, because of a mistake which has been reported, or because
 * binding it is not written yet. Nothing involving it is reported again, which keeps one mistake to one
 * message, as Roslyn's error types do: it converts to and from every type, and has no members to look for.
 * There is only the one, which is equal only to itself. */
internal sealed class ErrorType : SemanticType
{
    // Static fields.
    internal static ErrorType Instance { get; } = new();


    // Internal fields.
    internal override bool IsValueType => false;


    // Constructors.
    private ErrorType() : base(false) { }


    // Internal methods.
    internal override SemanticType Substitute(TypeSubstitution substitution)
    {
        return this;
    }

    internal override SemanticType WithNullableAnnotation(bool isNullableAnnotated)
    {
        return this;
    }


    // Inherited methods.
    public override bool Equals(SemanticType? other)
    {
        return ReferenceEquals(this, other);
    }

    public override int GetHashCode()
    {
        return 0;
    }

    /* Never part of a message, since nothing involving it is reported. */
    public override string ToString()
    {
        return KGVL.TYPE_NULLABLE_INDICATOR.ToString();
    }
}
