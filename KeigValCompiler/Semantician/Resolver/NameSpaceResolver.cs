using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

internal class NameSpaceResolver : IPackResolver
{
    // Private methods.
    private void ResolveNameSpaceIdentifiers(IEnumerable<PackNameSpace> nameSpaces)
    {
        foreach (PackNameSpace NameSpace in nameSpaces)
        {
            string Name = NameSpace.SelfIdentifier.SourceCodeName;
            NameSpace.SelfIdentifier.Target = NameSpace;

            NameSpace.SelfIdentifier.ResolvedName = Name;

            int SeparatorIndex = Name.LastIndexOf(KGVL.NAMESPACE_SEPARATOR);
            NameSpace.SelfIdentifier.SelfName = SeparatorIndex == -1 ? Name : Name.Substring(SeparatorIndex + 1);
        }
    }

    private void ResolveMemberNameSpaces(IEnumerable<PackNameSpace> nameSpaces)
    {
        foreach (PackNameSpace NameSpace in nameSpaces)
        {
            foreach (PackMember Member in NameSpace.AllMembers)
            {
                Member.NameSpace = NameSpace;
            }
        }
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ResolveNameSpaceIdentifiers(context.Pack.NameSpaces);
        ResolveMemberNameSpaces(context.Pack.NameSpaces);
    }
}