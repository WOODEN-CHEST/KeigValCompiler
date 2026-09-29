using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;
using System.Text;

namespace KeigValCompiler.Semantician.Types;

/* A class, structure, interface, enum or delegate, with the types given for its generic parameters. A
 * type declared inside another also carries that type as seen where it is used, since inside Outer<T>,
 * "Inner" is Outer<T>.Inner, and Outer<int>.Inner and Outer<string>.Inner are different types. Two are
 * the same type when they are the same declaration with the same type arguments inside the same
 * containing type. */
internal sealed class DeclaredType : SemanticType
{
    // Static fields.
    private const string SEPARATOR = ", ";


    // Internal fields.
    internal PackMember Declaration { get; private init; }
    internal IReadOnlyList<SemanticType> TypeArguments { get; private init; }

    /* The type holding the declaration, or null for one a namespace holds. */
    internal DeclaredType? ContainingType { get; private init; }

    /* Which type the compiler knows by name it is, such as Int32 or Array, or null for any other. */
    internal LibraryType? LibraryType { get; private init; }

    internal override bool IsValueType => Declaration is PackStruct or PackEnumeration;


    // Constructors.
    internal DeclaredType(PackMember declaration,
        IReadOnlyList<SemanticType> typeArguments,
        DeclaredType? containingType,
        LibraryType? libraryType,
        bool isNullableAnnotated) : base(isNullableAnnotated)
    {
        Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
        TypeArguments = typeArguments ?? throw new ArgumentNullException(nameof(typeArguments));
        ContainingType = containingType;
        LibraryType = libraryType;
    }


    // Internal methods.
    internal override SemanticType Substitute(TypeSubstitution substitution)
    {
        ArgumentNullException.ThrowIfNull(substitution, nameof(substitution));

        return new DeclaredType(Declaration,
            TypeArguments.Select(argument => argument.Substitute(substitution)).ToArray(),
            (DeclaredType?)ContainingType?.Substitute(substitution),
            LibraryType,
            IsNullableAnnotated);
    }

    internal override SemanticType WithNullableAnnotation(bool isNullableAnnotated)
    {
        return new DeclaredType(Declaration, TypeArguments, ContainingType, LibraryType,
            isNullableAnnotated && !IsValueType);
    }


    // Inherited methods.
    public override bool Equals(SemanticType? other)
    {
        return (other is DeclaredType OtherType)
            && ReferenceEquals(Declaration, OtherType.Declaration)
            && TypeArguments.SequenceEqual(OtherType.TypeArguments)
            && Equals(ContainingType, OtherType.ContainingType);
    }

    /* The declaration is hashed by reference, since PackMember compares by resolved name. The type
     * arguments' hashes leave their annotations out, as Equals does. */
    public override int GetHashCode()
    {
        HashCode Hash = new();
        Hash.Add(ReferenceEqualityComparer.Instance.GetHashCode(Declaration));
        foreach (SemanticType Argument in TypeArguments)
        {
            Hash.Add(Argument);
        }
        Hash.Add(ContainingType);
        return Hash.ToHashCode();
    }

    /* The type as source code would write it: "int[]", "Thing<string>?", "Outer<int>.Inner". */
    public override string ToString()
    {
        StringBuilder Builder = new();
        if (LibraryType == LibraryTypes.Array)
        {
            Builder.Append(TypeArguments[0]).Append(KGVL.OPEN_SQUARE_BRACKET).Append(KGVL.CLOSE_SQUARE_BRACKET);
        }
        else if (LibraryType == LibraryTypes.Nullable)
        {
            return TypeArguments[0].ToString() + KGVL.TYPE_NULLABLE_INDICATOR;
        }
        else if (LibraryType?.Keyword != null)
        {
            Builder.Append(LibraryType.Keyword);
        }
        else
        {
            if (ContainingType != null)
            {
                Builder.Append(ContainingType).Append(KGVL.MEMBER_ACCESS);
            }
            Builder.Append(Declaration.SelfIdentifier.SourceCodeName);
            if (TypeArguments.Count > 0)
            {
                Builder.Append(KGVL.GENERIC_TYPE_START).Append(string.Join(SEPARATOR, TypeArguments))
                    .Append(KGVL.GENERIC_TYPE_END);
            }
        }

        if (IsNullableAnnotated)
        {
            Builder.Append(KGVL.TYPE_NULLABLE_INDICATOR);
        }
        return Builder.ToString();
    }
}
