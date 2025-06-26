namespace KeigValCompiler.Semantician.Member;

internal abstract class PackMember : IIdentifiable
{
    // Static fields.
    public const int ACCESS_LEVEL_PRIVATE = 0;
    public const int ACCESS_LEVEL_PROTECTED = 1;
    public const int ACCESS_LEVEL_PUBLIC = 2;


    // Fields.
    public virtual Identifier SelfIdentifier { get; private init; }


    // Internal fields.
    internal virtual Identifier? ParentItem { get; set; }
    internal virtual PackNameSpace NameSpace { get; set; }
    internal virtual PackSourceFile SourceFile { get; set; }
    internal virtual DataPack Pack => SourceFile.Pack;
    internal virtual PackMemberModifiers Modifiers { get; set; }
    internal virtual IEnumerable<PackMember> SubMembers => Enumerable.Empty<PackMember>();
    internal virtual IEnumerable<PackMember> AllSubMembers => Enumerable.Empty<PackMember>();
    internal SourceFileOrigin SourceFileOrigin { get; set; }


    // Constructors.
    internal PackMember(Identifier identifier,
        PackSourceFile sourceFile)
    {
        SelfIdentifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        SourceFile = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));
    }


    // Internal static methods.
    internal static int GetAccessLevel(PackMemberModifiers modifiers)
    {
        int Level = ACCESS_LEVEL_PRIVATE;

        if ((modifiers & PackMemberModifiers.Protected) > 0)
        {
            Level = ACCESS_LEVEL_PROTECTED;
        }
        else if ((modifiers & PackMemberModifiers.Public) > 0)
        {
            Level = ACCESS_LEVEL_PUBLIC;
        }

        return Level;
    }


    // Internal methods.
    internal int CountAccessModifiers()
    {
        return ((Modifiers & PackMemberModifiers.Private) > 0 ? 1 : 0)
            + ((Modifiers & PackMemberModifiers.Protected) > 0 ? 1 : 0)
            + ((Modifiers & PackMemberModifiers.Public) > 0 ? 1 : 0);
    }

    internal bool HasAnyModifier(params PackMemberModifiers[] targets)
    {
        foreach (PackMemberModifiers Target in targets)
        {
            if ((Modifiers & Target) > 0)
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasAnyModifiersExcept(params PackMemberModifiers[] allowedModifiers)
    {
        PackMemberModifiers CombinedModifiers = PackMemberModifiers.None;
        foreach (PackMemberModifiers AllowedModifier in allowedModifiers)
        {
            CombinedModifiers |= AllowedModifier;
        }

        return (CombinedModifiers | Modifiers) != CombinedModifiers;
    }

    internal bool HasModifier(PackMemberModifiers modifier) => (Modifiers & modifier) != PackMemberModifiers.None;


    // Inherited methods.
    public override int GetHashCode()
    {
        return SelfIdentifier.ToString().GetHashCode(); // Random number go!
    }

    public override bool Equals(object? obj)
    {
        if (obj is PackMember MemberObj)
        {
            return SelfIdentifier.Equals(MemberObj?.SelfIdentifier);
        }
        return false;
    }

    public override string ToString()
    {
        return SelfIdentifier.ToString();
    }
}