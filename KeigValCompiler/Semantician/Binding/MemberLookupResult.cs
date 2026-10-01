using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* What C#'s member lookup of a name found: one member, a group of functions of the name, which the call
 * choosing among them decides, members which none hides, which is ambiguous, or nothing. When nothing which can
 * be used from where the name is written was found, a member which cannot be may still have been, or one of the
 * name with another number of generic parameters, which make for a better error. An enum's constant is found
 * apart, since it is no member of its own. */
internal sealed class MemberLookupResult
{
    // Internal fields.
    internal IReadOnlyList<FoundMember> Members { get; private init; } = Array.Empty<FoundMember>();

    /* A member of the name which the use cannot reach, when nothing it can reach was found. */
    internal PackMember? InaccessibleMember { get; private init; }

    /* A member of the name with another number of generic parameters than the name is written with, when
     * nothing was found. */
    internal PackMember? OtherArityMember { get; private init; }

    /* The enum constant of the name, with the enum's type, for a lookup in an enum. */
    internal PackEnumerationConstant? EnumConstant { get; private init; }
    internal DeclaredType? EnumType { get; private init; }

    internal bool IsFound => (Members.Count > 0) || (EnumConstant != null);
    internal bool IsFunctionGroup => (Members.Count > 0)
        && Members.All(found => found.Member is PackFunction);
    internal bool IsAmbiguous => (Members.Count > 1) && !IsFunctionGroup;

    /* The one member found, when it is not a function, which a group of one still is. */
    internal FoundMember? SingleMember => ((Members.Count == 1) && !IsFunctionGroup) ? Members[0] : null;


    // Constructors.
    private MemberLookupResult() { }


    // Internal static methods.
    internal static MemberLookupResult Found(IReadOnlyList<FoundMember> members)
    {
        ArgumentNullException.ThrowIfNull(members, nameof(members));
        return new() { Members = members };
    }

    internal static MemberLookupResult NotFound(PackMember? inaccessibleMember, PackMember? otherArityMember)
    {
        return new() { InaccessibleMember = inaccessibleMember, OtherArityMember = otherArityMember };
    }

    internal static MemberLookupResult FoundEnumConstant(PackEnumerationConstant constant, DeclaredType enumType)
    {
        return new()
        {
            EnumConstant = constant ?? throw new ArgumentNullException(nameof(constant)),
            EnumType = enumType ?? throw new ArgumentNullException(nameof(enumType))
        };
    }
}
