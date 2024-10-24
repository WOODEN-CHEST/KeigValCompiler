namespace KeigValCompiler.Semantician.Member;

internal abstract class PackMember : IIdentifiable
{
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


    // Internal static functions.
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

    internal bool HasModifier(PackMemberModifiers modifier) => (Modifiers | modifier) > 0;


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