using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Types;

/* A generic parameter used as a type, as "T" is inside Thing<T>. Two are the same type when they are the
 * same parameter of the same declaration. A '?' on one is kept as an annotation unless the parameter is
 * constrained to structures, which makes it Nullable<T> instead. What "T?" means for a parameter which may
 * be either kind of type is still an open question, and it is an annotation until that is decided. */
internal sealed class GenericParameterType : SemanticType
{
    // Internal fields.
    internal GenericTypeParameter Parameter { get; private init; }

    internal override bool IsValueType => Parameter.Constraints.Any(
        constraint => constraint.SpecialConstraint == SpecialGenericConstraint.Struct);


    // Constructors.
    internal GenericParameterType(GenericTypeParameter parameter, bool isNullableAnnotated)
        : base(isNullableAnnotated)
    {
        Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
    }


    // Internal methods.
    /* The type standing for the parameter keeps its annotation, which the parameter's own adds to unless
     * the type is a value type, which a '?' cannot annotate. */
    internal override SemanticType Substitute(TypeSubstitution substitution)
    {
        ArgumentNullException.ThrowIfNull(substitution, nameof(substitution));

        SemanticType? Replacement = substitution.GetReplacement(Parameter);
        if (Replacement == null)
        {
            return this;
        }
        return IsNullableAnnotated ? Replacement.WithNullableAnnotation(true) : Replacement;
    }

    internal override SemanticType WithNullableAnnotation(bool isNullableAnnotated)
    {
        return new GenericParameterType(Parameter, isNullableAnnotated && !IsValueType);
    }


    // Inherited methods.
    public override bool Equals(SemanticType? other)
    {
        return (other is GenericParameterType OtherType) && ReferenceEquals(Parameter, OtherType.Parameter);
    }

    /* The parameter is hashed by reference, since GenericTypeParameter compares by name and constraints. */
    public override int GetHashCode()
    {
        return ReferenceEqualityComparer.Instance.GetHashCode(Parameter);
    }

    public override string ToString()
    {
        return IsNullableAnnotated ? Parameter.SelfIdentifier.SourceCodeName + KGVL.TYPE_NULLABLE_INDICATOR
            : Parameter.SelfIdentifier.SourceCodeName;
    }
}
