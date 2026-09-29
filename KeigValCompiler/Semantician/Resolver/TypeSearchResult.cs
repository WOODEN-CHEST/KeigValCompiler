using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* What looking a type name up found: the type or generic parameter it means, several imported types
 * it could equally mean, or nothing. When nothing matched, a type of that name with another number of
 * generic parameters, or a matching one which cannot be seen from the use, may still have been seen,
 * which makes for a better error. */
internal sealed class TypeSearchResult
{
    // Internal fields.
    internal IIdentifiable? Target { get; private init; }
    internal IReadOnlyList<PackMember> AmbiguousTypes { get; private init; } = Array.Empty<PackMember>();

    /* The generic parameter count of a same named type which was found with the wrong one. */
    internal int? OtherGenericParameterCount { get; private init; }

    /* A type which matched but cannot be seen where the name is written, when nothing else matched. */
    internal PackMember? InaccessibleType { get; private init; }

    internal bool IsFound => Target != null;
    internal bool IsAmbiguous => AmbiguousTypes.Count > 1;


    // Constructors.
    private TypeSearchResult() { }


    // Internal static methods.
    internal static TypeSearchResult Found(IIdentifiable target)
    {
        return new() { Target = target ?? throw new ArgumentNullException(nameof(target)) };
    }

    internal static TypeSearchResult Ambiguous(IReadOnlyList<PackMember> types)
    {
        return new() { AmbiguousTypes = types ?? throw new ArgumentNullException(nameof(types)) };
    }

    internal static TypeSearchResult NotFound(int? otherGenericParameterCount, PackMember? inaccessibleType)
    {
        return new()
        {
            OtherGenericParameterCount = otherGenericParameterCount,
            InaccessibleType = inaccessibleType
        };
    }
}
