using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler;

internal class DefaultIdentifierSearcher : IIdentifierSearcher
{
    // Private methods.
    private PackMember? SearchSourceFileForTypeByCodeName(string codeName, PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        PackMember? TargetType = registry.GetTypeFromShorthandName(codeName);
        if (TargetType != null)
        {
            return TargetType;
        }

        foreach (PackNameSpace WorkingNameSpace in sourceFile.Namespaces)
        {
            foreach (PackNameSpace FilterNameSpace in sourceFile.NamespaceImports.Concat(new PackNameSpace[] { WorkingNameSpace }))
            {
                PackMember? FoundType = SearchNameSpaceForTypeByCodeName(codeName, FilterNameSpace);
                if (FoundType != null)
                {
                    return FoundType;
                }
            }
        }
        return null;
    }

    private PackMember? SearchNameSpaceForTypeByCodeName(string codeName, PackNameSpace nameSpace)
    {
        foreach (PackMember TypeMember in nameSpace.Types)
        {
            if ((TypeMember.SelfIdentifier.SelfName == codeName)
                || (TypeMember.SelfIdentifier.ResolvedName == codeName))
            {
                return TypeMember;
            }
        }
        return null;
    }


    // Inherited methods.
    public PackMember? GetTypeFromCodeName(string codeName, PackSourceFile sourceFile, BuiltInTypeRegistry registry)
    {
        return SearchSourceFileForTypeByCodeName(codeName, sourceFile, registry);
    }
}