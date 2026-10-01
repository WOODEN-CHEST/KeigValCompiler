using KeigValCompiler.Semantician.Bound;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* What a name on its own stands for where it is written, as looking it up found, before anything about it is
 * reported, so that a meaning can be weighed against another before it is bound, as C#'s "Color Color" rule
 * needs. Only what its kind needs is set. When nothing was found, a member which cannot be used here, or one of
 * the name with another number of generic parameters, may still have been seen, which makes for a better error. */
internal sealed class SimpleNameMeaning
{
    // Internal fields.
    internal SimpleNameMeaningKind Kind { get; private init; }

    /* A local, and the block declaring it, which says whether binding has reached its declaration. */
    internal LocalSymbol? Local { get; private init; }
    internal BlockScope? LocalBlock { get; private init; }

    /* A parameter, with its type, and whether it can be used where the name is: a record's positional
     * parameter is seen in a static member's starting value, but cannot be used there. */
    internal FunctionParameter? Parameter { get; private init; }
    internal SemanticType? ParameterType { get; private init; }
    internal bool IsParameterUsable { get; private init; }

    internal GenericTypeParameter? GenericParameter { get; private init; }

    /* A type a namespace holds, or the library type a keyword stands for. */
    internal PackMember? TypeDeclaration { get; private init; }

    /* What member lookup found, in a type around the name or at a namespace, and for a type, whether it was
     * the innermost type around, whose object the code runs on. */
    internal MemberLookupResult? Members { get; private init; }
    internal bool IsInnermostType { get; private init; }

    /* A namespace's full name. */
    internal string? NameSpaceName { get; private init; }

    /* The full names the imported namespaces offering the name give it. */
    internal IReadOnlyList<string> AmbiguousCandidates { get; private init; } = Array.Empty<string>();

    /* When nothing was found: a member or a type which cannot be used here, a member of the name with another
     * number of generic parameters, or the number a type of the name has. */
    internal PackMember? InaccessibleMember { get; private init; }
    internal PackMember? OtherArityMember { get; private init; }
    internal int? OtherTypeArity { get; private init; }

    /* When nothing was found for a name written with type arguments, what kind of thing taking none, other than
     * a member or a type, has the name: a local, a parameter, a generic parameter or a namespace, or null. */
    internal string? NonGenericKind { get; private init; }

    /* When nothing was found, whether a type around has bases which could not be resolved, from which the name
     * might have been inherited. */
    internal bool IsInheritanceUnknown { get; private init; }


    // Constructors.
    private SimpleNameMeaning(SimpleNameMeaningKind kind)
    {
        Kind = kind;
    }


    // Internal static methods.
    internal static SimpleNameMeaning ForLocal(LocalSymbol local, BlockScope block)
    {
        return new(SimpleNameMeaningKind.Local)
        {
            Local = local ?? throw new ArgumentNullException(nameof(local)),
            LocalBlock = block ?? throw new ArgumentNullException(nameof(block))
        };
    }

    internal static SimpleNameMeaning ForParameter(FunctionParameter parameter, SemanticType type, bool isUsable)
    {
        return new(SimpleNameMeaningKind.Parameter)
        {
            Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter)),
            ParameterType = type ?? throw new ArgumentNullException(nameof(type)),
            IsParameterUsable = isUsable
        };
    }

    internal static SimpleNameMeaning ForGenericParameter(GenericTypeParameter parameter)
    {
        return new(SimpleNameMeaningKind.GenericParameter)
        {
            GenericParameter = parameter ?? throw new ArgumentNullException(nameof(parameter))
        };
    }

    /* A keyword's library type, or null when the library does not declare it, which has been reported. */
    internal static SimpleNameMeaning ForKeywordType(PackMember? declaration)
    {
        return new(SimpleNameMeaningKind.KeywordType) { TypeDeclaration = declaration };
    }

    internal static SimpleNameMeaning ForTypeMember(MemberLookupResult members, bool isInnermostType)
    {
        return new(SimpleNameMeaningKind.TypeMember)
        {
            Members = members ?? throw new ArgumentNullException(nameof(members)),
            IsInnermostType = isInnermostType
        };
    }

    internal static SimpleNameMeaning ForNameSpaceMember(MemberLookupResult members)
    {
        return new(SimpleNameMeaningKind.NameSpaceMember)
        {
            Members = members ?? throw new ArgumentNullException(nameof(members))
        };
    }

    internal static SimpleNameMeaning ForNameSpaceType(PackMember declaration)
    {
        return new(SimpleNameMeaningKind.NameSpaceType)
        {
            TypeDeclaration = declaration ?? throw new ArgumentNullException(nameof(declaration))
        };
    }

    internal static SimpleNameMeaning ForNameSpace(string nameSpaceName)
    {
        return new(SimpleNameMeaningKind.NameSpace)
        {
            NameSpaceName = nameSpaceName ?? throw new ArgumentNullException(nameof(nameSpaceName))
        };
    }

    internal static SimpleNameMeaning ForImportAmbiguity(IReadOnlyList<string> candidates)
    {
        return new(SimpleNameMeaningKind.ImportAmbiguity)
        {
            AmbiguousCandidates = candidates ?? throw new ArgumentNullException(nameof(candidates))
        };
    }

    internal static SimpleNameMeaning ForNotFound(PackMember? inaccessibleMember,
        PackMember? otherArityMember,
        int? otherTypeArity,
        string? nonGenericKind,
        bool isInheritanceUnknown)
    {
        return new(SimpleNameMeaningKind.NotFound)
        {
            InaccessibleMember = inaccessibleMember,
            OtherArityMember = otherArityMember,
            OtherTypeArity = otherTypeArity,
            NonGenericKind = nonGenericKind,
            IsInheritanceUnknown = isInheritanceUnknown
        };
    }
}
