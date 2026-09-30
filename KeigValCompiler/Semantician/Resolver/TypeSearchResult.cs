using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* What looking a type name up found: the type or generic parameter it means, a namespace, several types
 * it could equally mean, imported ones or ones inherited from interfaces, or nothing. A namespace is found
 * by a name which may start a qualified one, as the "KGVL" of "KGVL.Int32" does, and is the wrong thing
 * for a name which ends one. When nothing matched, a type of that name with another number of generic
 * parameters, or a matching one which cannot be seen from the use, may still have been seen, which makes
 * for a better error. A lookup which needed the bases of a class or structure whose own base list was
 * still being resolved ends there, with neither. */
internal sealed class TypeSearchResult
{
    // Internal fields.
    internal IIdentifiable? Target { get; private init; }
    internal IReadOnlyList<PackMember> AmbiguousTypes { get; private init; } = Array.Empty<PackMember>();

    /* The generic parameter count of a same named type which was found with the wrong one. */
    internal int? OtherGenericParameterCount { get; private init; }

    /* A type which matched but cannot be seen where the name is written, when nothing else matched. */
    internal PackMember? InaccessibleType { get; private init; }

    /* The full name of the namespace found, when a namespace was found rather than a type. */
    internal string? NameSpaceName { get; private init; }

    /* A namespace which a name written with type arguments would have named without them, when nothing else
     * matched. */
    internal string? NameSpaceWithArguments { get; private init; }

    /* The type through whose bases the type found, or the ones found equally, were reached: the qualifier,
     * for a name after one, or else the type around the use. */
    internal PackMember? InheritedThrough { get; private init; }

    /* The bases written on the way from InheritedThrough to the type declaring the one found, once the way
     * has been worked out. */
    internal IReadOnlyList<TypeTargetIdentifier>? InheritedPath { get; private init; }

    /* The class or structure whose inherited types the lookup needed while its own base list was being
     * resolved, which makes the bases of the two types depend on each other. */
    internal PackMember? CircularType { get; private init; }

    /* A namespace of the same name as the type found, declared on the same side of the standard library's
     * boundary, which is a clash reported on its own. A name after this one is looked for in it too, so that
     * nothing more is reported about the clash. */
    internal string? ClashingNameSpaceName { get; private init; }

    internal bool IsFound => Target != null;
    internal bool IsNameSpace => NameSpaceName != null;
    internal bool IsAmbiguous => AmbiguousTypes.Count > 1;


    // Constructors.
    private TypeSearchResult() { }


    // Internal static methods.
    internal static TypeSearchResult Found(IIdentifiable target)
    {
        return new() { Target = target ?? throw new ArgumentNullException(nameof(target)) };
    }

    internal static TypeSearchResult FoundInherited(PackMember type, PackMember inheritedThrough)
    {
        return new()
        {
            Target = type ?? throw new ArgumentNullException(nameof(type)),
            InheritedThrough = inheritedThrough ?? throw new ArgumentNullException(nameof(inheritedThrough))
        };
    }

    internal static TypeSearchResult AmbiguousInherited(IReadOnlyList<PackMember> types,
        PackMember inheritedThrough)
    {
        return new()
        {
            AmbiguousTypes = types ?? throw new ArgumentNullException(nameof(types)),
            InheritedThrough = inheritedThrough ?? throw new ArgumentNullException(nameof(inheritedThrough))
        };
    }

    internal static TypeSearchResult FoundClashing(PackMember type, string clashingNameSpaceName)
    {
        return new()
        {
            Target = type ?? throw new ArgumentNullException(nameof(type)),
            ClashingNameSpaceName = clashingNameSpaceName
                ?? throw new ArgumentNullException(nameof(clashingNameSpaceName))
        };
    }

    internal static TypeSearchResult FoundNameSpace(string nameSpaceName)
    {
        return new() { NameSpaceName = nameSpaceName ?? throw new ArgumentNullException(nameof(nameSpaceName)) };
    }

    internal static TypeSearchResult Ambiguous(IReadOnlyList<PackMember> types)
    {
        return new() { AmbiguousTypes = types ?? throw new ArgumentNullException(nameof(types)) };
    }

    internal static TypeSearchResult Circular(PackMember circularType)
    {
        return new() { CircularType = circularType ?? throw new ArgumentNullException(nameof(circularType)) };
    }

    internal static TypeSearchResult NotFound(int? otherGenericParameterCount,
        PackMember? inaccessibleType,
        string? nameSpaceWithArguments)
    {
        return new()
        {
            OtherGenericParameterCount = otherGenericParameterCount,
            InaccessibleType = inaccessibleType,
            NameSpaceWithArguments = nameSpaceWithArguments
        };
    }


    // Internal methods.
    /* The same type found through bases, with the way to it. */
    internal TypeSearchResult WithInheritedPath(IReadOnlyList<TypeTargetIdentifier>? inheritedPath)
    {
        return new() { Target = Target, InheritedThrough = InheritedThrough, InheritedPath = inheritedPath };
    }
}
