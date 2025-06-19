using KeigValCompiler.Semantician.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Resolver;

internal class ParentItemResolver : IPackResolver
{
    // Private methods.
    private void ResolveParentItems(IEnumerable<PackNameSpace> nameSpaces)
    {
        foreach (PackNameSpace NameSpace in nameSpaces)
        {
            foreach (PackMember Member in NameSpace.Members)
            {
                ResolveParentItemsOfMember(Member, null);
            }
        }
    }

    private void ResolveParentItemsOfMember(PackMember member, Identifier? parentItem)
    {
        member.ParentItem = parentItem;

        foreach (PackMember SubMember in member.SubMembers)
        {
            ResolveParentItemsOfMember(SubMember, member.SelfIdentifier);
        }
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ResolveParentItems(context.Pack.NameSpaces);
    }
}