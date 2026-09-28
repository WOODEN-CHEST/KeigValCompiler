using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Library;

/* Writes the signatures of one library type's members, so that a binding reads close to the
 * declaration it matches: Member.StaticMethod(PARSE, Member.Self, LibraryTypes.String) for
 * "static int Parse(string text)" in Int32. */
internal class SignatureBuilder
{
    // Internal fields.
    internal LibraryType DeclaringType { get; private init; }

    /* The declaring type as its members name it: Int32 in Int32, and Array<T> in Array<T>. */
    internal SignatureType Self { get; private init; }


    // Constructors.
    internal SignatureBuilder(LibraryType declaringType)
    {
        DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));

        SignatureType[] OwnParameters = Enumerable.Range(0, declaringType.GenericParameterNames.Count)
            .Select(SignatureType.TypeParameter).ToArray();
        Self = SignatureType.Of(declaringType, OwnParameters);
    }


    // Internal methods.
    /* One of the declaring type's generic parameters, as T is 0 in Array<T>. */
    internal SignatureType TypeParameter(int position)
    {
        return SignatureType.TypeParameter(position);
    }

    /* Methods. A null return type is void. */
    internal MemberSignature Method(string name, SignatureType? returnType, params SignatureParameter[] parameters)
    {
        return Create(MemberSignatureKind.Method, name, null, false, returnType, parameters);
    }

    internal MemberSignature StaticMethod(string name,
        SignatureType? returnType,
        params SignatureParameter[] parameters)
    {
        return Create(MemberSignatureKind.Method, name, null, true, returnType, parameters);
    }

    internal MemberSignature Constructor(params SignatureParameter[] parameters)
    {
        return Create(MemberSignatureKind.Constructor, null, null, false, null, parameters);
    }

    /* Operators, which are always static. */
    internal MemberSignature Operator(OverloadableOperator overloadedOperator,
        SignatureType returnType,
        params SignatureParameter[] parameters)
    {
        return Create(MemberSignatureKind.Operator, null, overloadedOperator, true, returnType, parameters);
    }

    /* An operator on one of the declaring type, giving the same type back, as ++ or unary -. */
    internal MemberSignature UnaryOperator(OverloadableOperator overloadedOperator)
    {
        return Operator(overloadedOperator, Self, Self);
    }

    /* An operator on two of the declaring type, giving the same type back, as + or &. */
    internal MemberSignature BinaryOperator(OverloadableOperator overloadedOperator)
    {
        return Operator(overloadedOperator, Self, Self, Self);
    }

    /* A comparison of two of the declaring type, giving a bool, as == or <. */
    internal MemberSignature Comparison(OverloadableOperator overloadedOperator)
    {
        return Operator(overloadedOperator, LibraryTypes.Boolean, Self, Self);
    }

    /* A shift of the declaring type by an int, giving the same type back. */
    internal MemberSignature Shift(OverloadableOperator overloadedOperator)
    {
        return Operator(overloadedOperator, Self, Self, LibraryTypes.Int32);
    }

    internal MemberSignature Conversion(bool isImplicit, SignatureType from, SignatureType to)
    {
        return Operator(isImplicit ? OverloadableOperator.ImplicitCast : OverloadableOperator.ExplicitCast,
            to, from);
    }

    /* Accessors, whose signature carries the type of the property or indexer they belong to. */
    internal MemberSignature PropertyGetter(string name, SignatureType type)
    {
        return Create(MemberSignatureKind.PropertyGetter, name, null, false, type,
            Array.Empty<SignatureParameter>());
    }

    internal MemberSignature IndexerGetter(SignatureType type, params SignatureParameter[] parameters)
    {
        return Create(MemberSignatureKind.IndexerGetter, null, null, false, type, parameters);
    }

    internal MemberSignature IndexerSetter(SignatureType type, params SignatureParameter[] parameters)
    {
        return Create(MemberSignatureKind.IndexerSetter, null, null, false, type, parameters);
    }


    // Private methods.
    private MemberSignature Create(MemberSignatureKind kind,
        string? name,
        OverloadableOperator? overloadedOperator,
        bool isStatic,
        SignatureType? type,
        SignatureParameter[] parameters)
    {
        return new()
        {
            DeclaringType = DeclaringType,
            Kind = kind,
            Name = name,
            Operator = overloadedOperator,
            IsStatic = isStatic,
            Type = type,
            Parameters = parameters
        };
    }
}
