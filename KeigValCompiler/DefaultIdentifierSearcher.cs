using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Member;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler;

internal class DefaultIdentifierSearcher : IIdentifierSearcher
{
    // Private fields.
    private IdentifierGenerator _identifierGenerator;


    // Constructors.
    public DefaultIdentifierSearcher(IdentifierGenerator identifierGenerator)
    {
        _identifierGenerator = identifierGenerator ?? throw new ArgumentNullException(nameof(identifierGenerator));
    }


    // Private methods.
    private PackMember? SearchNameSpaceForTypeByCodeName(string codeName, PackNameSpace nameSpace)
    {
        foreach (PackMember TypeMember in nameSpace.AllTypes)
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
        PackMember? TargetType = registry.GetTypeFromShorthandName(codeName);
        if (TargetType != null)
        {
            return TargetType;
        }

        foreach (PackNameSpace NameSpace in sourceFile.Namespaces.Concat(sourceFile.NamespaceImports))
        {
            PackMember? FoundType = SearchNameSpaceForTypeByCodeName(codeName, NameSpace);
            if (FoundType != null)
            {
                return FoundType;
            }
        }

        return null;
    }

    public PackFunction? GetFunctionFromCodeName(string codeName,
        IPackFunctionHolder? primarySearchTarget,
        bool isContextStatic,
        PackSourceFile sourceFile)
    {
        //PackMemberModifiers TargetStaticModifier = isContextStatic ? PackMemberModifiers.Static : PackMemberModifiers.None;

        //foreach (PackFunction Function in primarySearchTarget?.Functions ?? Enumerable.Empty<PackFunction>())
        //{
        //    bool DoesNameMatch = (Function.SelfIdentifier.SelfName == codeName)
        //        || (Function.SelfIdentifier.ResolvedName == codeName)
        //        || ();
        //    bool DoesStaticModifierMatch = !isContextStatic || Function.HasModifier(PackMemberModifiers.Static);
        //    bool DoesAccessMatch = PackMember.GetAccessLevel();

        //    if (DoesNameMatch && DoesStaticModifierMatch)
        //    {
        //        return Function;
        //    }
        //}

        throw new NotImplementedException();
    }
}