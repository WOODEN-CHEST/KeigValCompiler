using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks that no two functions, constructors, indexers or operators one type or namespace declares take the
 * same parameters, as C# compares them: by their numbers of generic parameters, their parameters' types,
 * and which parameters are passed by reference, but not by return types, parameter names, "params", or
 * "ref", "out" and "in" against each other. Two conversions between the same two types clash whether each
 * is implicit or explicit, and however each takes its parameter, and so do two explicit implementations of
 * the same property. An explicit implementation is only compared with the others implementing the same
 * interface. Members of other kinds sharing a name are DeclarationNameChecker's to report. */
internal class DuplicateSignatureChecker : IPackResolver
{
    // Private methods.
    /* Each member is compared with those declared before it, across files, so the later of two is the one
     * reported. */
    private void CheckHolder(object holder,
        string holderName,
        Dictionary<PackSourceFile, int> fileOrder,
        PackResolutionContext context)
    {
        IEnumerable<PackMember> Members = MemberRelations.GetSignedMembers(holder)
            .Where(member => (member is PackFunction or PackIndexer)
                || ((member is PackProperty) && (MemberRelations.GetExplicitInterface(member) != null)))
            .OrderBy(member => fileOrder[member.SourceFile])
            .ThenBy(member => member.SourceFileOrigin.Line);

        List<DeclaredSignature> Earlier = new();
        TypeSubstitution NoSubstitution = new();
        foreach (PackMember Member in Members)
        {
            DeclaredSignature Signature = context.SignatureReader.Read(Member, NoSubstitution);
            if (!Signature.IsComplete)
            {
                continue;
            }

            DeclaredSignature? Clash = Earlier.FirstOrDefault(earlier => IsClash(earlier, Signature, context));
            if (Clash != null)
            {
                ReportClash(Signature, Clash, holderName, context);
            }
            Earlier.Add(Signature);
        }
    }

    private bool IsClash(DeclaredSignature first, DeclaredSignature second, PackResolutionContext context)
    {
        if ((first.Kind != second.Kind) || !IsSameExplicitInterface(first.Member, second.Member, context))
        {
            return false;
        }

        return first.Kind switch
        {
            DeclaredSignatureKind.Method => (first.Name == second.Name) && first.HasSameParameters(second, false),
            DeclaredSignatureKind.Constructor => (first.Member.HasModifier(PackMemberModifiers.Static)
                == second.Member.HasModifier(PackMemberModifiers.Static))
                && first.HasSameParameters(second, false),
            DeclaredSignatureKind.Property => first.Name == second.Name,
            DeclaredSignatureKind.Indexer => first.HasSameParameters(second, false),
            DeclaredSignatureKind.Operator => (first.Operator == second.Operator)
                && first.HasSameParameters(second, false),
            DeclaredSignatureKind.Conversion => first.HasSameParameterTypes(second) && first.HasSameType(second),
            _ => false
        };
    }

    /* Two members implement the same interface explicitly, or neither does. One whose interface could not be
     * read, or which names it as an array or with '?', is compared with nothing, since what it names has
     * been reported. */
    private bool IsSameExplicitInterface(PackMember first, PackMember second, PackResolutionContext context)
    {
        TypeTargetIdentifier? FirstInterface = MemberRelations.GetExplicitInterface(first);
        TypeTargetIdentifier? SecondInterface = MemberRelations.GetExplicitInterface(second);
        if ((FirstInterface == null) || (SecondInterface == null))
        {
            return (FirstInterface == null) && (SecondInterface == null);
        }
        if (FirstInterface.IsArrayOrNullable || SecondInterface.IsArrayOrNullable)
        {
            return false;
        }

        SemanticType? FirstType = context.TypeReader.Read(FirstInterface);
        return (FirstType != null) && FirstType.Equals(context.TypeReader.Read(SecondInterface));
    }

    private void ReportClash(DeclaredSignature signature,
        DeclaredSignature earlier,
        string holderName,
        PackResolutionContext context)
    {
        PackMember Member = signature.Member;
        string Kind = MemberRelations.GetKindName(Member);
        string Name = MemberRelations.GetDisplayName(Member);
        string EarlierPath = earlier.Member.SourceFile.Path;
        int EarlierLine = earlier.Member.SourceFileOrigin.Line;

        if (signature.Kind == DeclaredSignatureKind.Conversion)
        {
            context.AddError(context.ErrorCreator.DuplicateConversion.CreateOptions(Name, holderName, EarlierPath,
                EarlierLine), Member);
        }
        else if (signature.Kind == DeclaredSignatureKind.Property)
        {
            context.AddError(context.ErrorCreator.DuplicateMemberName.CreateOptions(Kind, Name,
                MemberRelations.GetKindName(earlier.Member), EarlierPath, EarlierLine, holderName), Member);
        }
        else
        {
            context.AddError(context.ErrorCreator.DuplicateSignature.CreateOptions(Kind, Name, EarlierPath,
                EarlierLine), Member);
        }
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        Dictionary<PackSourceFile, int> FileOrder = new();
        foreach (PackSourceFile SourceFile in context.Pack.SourceFiles)
        {
            FileOrder.Add(SourceFile, FileOrder.Count);
        }

        foreach (PackNameSpace NameSpace in context.Pack.NameSpaces)
        {
            CheckHolder(NameSpace, NameSpace.SelfIdentifier.SourceCodeName, FileOrder, context);
        }
        foreach (PackMember Type in context.Pack.Types)
        {
            CheckHolder(Type, MemberRelations.GetDisplayName(Type), FileOrder, context);
        }
    }
}
