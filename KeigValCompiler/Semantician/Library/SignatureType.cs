namespace KeigValCompiler.Semantician.Library;

/* A type as a signature names it: a known type with its type arguments, or a generic parameter by its
 * position. Arrays and nullable value types are the library types they stand for, so int[] is
 * Array<int> and int? is Nullable<int>. A '?' on a reference type only annotates it, so it is no part
 * of a signature, just as in C#. Two are equal when they name the same type. */
internal sealed class SignatureType : IEquatable<SignatureType>
{
    // Internal fields.
    /* The type, or null for a generic parameter. */
    internal LibraryType? Type { get; private init; }
    internal IReadOnlyList<SignatureType> TypeArguments { get; private init; }
    internal bool IsGenericParameter => Type == null;

    /* Which generic parameter, for one: of the type declaring the member, or of the member itself. */
    internal int GenericParameterPosition { get; private init; }
    internal bool IsMethodGenericParameter { get; private init; }


    // Constructors.
    private SignatureType(LibraryType? type,
        IReadOnlyList<SignatureType> typeArguments,
        int genericParameterPosition,
        bool isMethodGenericParameter)
    {
        Type = type;
        TypeArguments = typeArguments;
        GenericParameterPosition = genericParameterPosition;
        IsMethodGenericParameter = isMethodGenericParameter;
    }


    // Internal static methods.
    internal static SignatureType Of(LibraryType type, params SignatureType[] typeArguments)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(typeArguments, nameof(typeArguments));

        if (typeArguments.Length != type.GenericParameterNames.Count)
        {
            throw new ArgumentException($"{type} takes {type.GenericParameterNames.Count} type arguments, "
                + $"not {typeArguments.Length}.", nameof(typeArguments));
        }
        return new(type, typeArguments, 0, false);
    }

    internal static SignatureType ArrayOf(SignatureType elementType)
    {
        return Of(LibraryTypes.Array, elementType);
    }

    internal static SignatureType NullableOf(SignatureType valueType)
    {
        return Of(LibraryTypes.Nullable, valueType);
    }

    /* A generic parameter of the type declaring the member, as T is 0 in Array<T>. */
    internal static SignatureType TypeParameter(int position)
    {
        return new(null, Array.Empty<SignatureType>(), position, false);
    }

    /* A generic parameter of the member itself, as T is 0 in Convert<T>(T value). */
    internal static SignatureType MethodParameter(int position)
    {
        return new(null, Array.Empty<SignatureType>(), position, true);
    }


    // Inherited methods.
    public bool Equals(SignatureType? other)
    {
        return (other != null)
            && (Type == other.Type)
            && (GenericParameterPosition == other.GenericParameterPosition)
            && (IsMethodGenericParameter == other.IsMethodGenericParameter)
            && TypeArguments.SequenceEqual(other.TypeArguments);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as SignatureType);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, GenericParameterPosition, IsMethodGenericParameter, TypeArguments.Count);
    }

    public override string ToString()
    {
        return SignatureFormatter.FormatType(this, null);
    }


    // Operators.
    /* So that a binding can name a type which takes no type arguments as just its LibraryType. */
    public static implicit operator SignatureType(LibraryType type) => Of(type);
}
