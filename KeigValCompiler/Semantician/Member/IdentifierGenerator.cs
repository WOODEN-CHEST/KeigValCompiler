using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal class IdentifierGenerator
{
    // Static fields.
    public const string FUNC_NAME_INIT = "init";
    public const string FUNC_NAME_GET = "get";
    public const string FUNC_NAME_SET = "set";


    // Methods.
    public string GetFullResolvedIdentifier(PackMember member)
    {
        if (member.ParentItem != null)
        {
            return member.ParentItem.ResolvedName
                + KGVL.NAMESPACE_SEPARATOR
                + member.SelfIdentifier.SourceCodeName;
        }
        else
        {
            return member.NameSpace.SelfIdentifier.ResolvedName
                + KGVL.NAMESPACE_SEPARATOR
                + member.SelfIdentifier.SourceCodeName;
        } 
    }

    public string GetFullyResolvedFunctionIdentifier(PackFunction function)
    {
        return GetFullResolvedIdentifier(function)
            + KGVL.IDENTIFIER_SEPARATOR_FUNCTION
            + function.Parameters.ToString();
    }

    public string GetPropertyFunctionIdentifier(PackProperty property, PackFunction function)
    {
        string FuncName;
        if (function == property.SetFunction)
        {
            FuncName = FUNC_NAME_SET;
        }
        else if(function == property.GetFunction)
        {
            FuncName = FUNC_NAME_GET;
        }
        else
        {
            FuncName =FUNC_NAME_INIT;
        }

        return property.SelfIdentifier.ResolvedName
            + KGVL.IDENTIFIER_ACCESSOR
            + FuncName;
    }

    public string GetIndexerFunctionIdentifier(PackIndexer indexer, PackFunction function)
    {
        string FuncName;
        if (function == indexer.SetFunction)
        {
            FuncName = FUNC_NAME_SET;
        }
        else
        {
            FuncName = FUNC_NAME_GET;
        }

        return indexer.SelfIdentifier.ResolvedName
            + KGVL.IDENTIFIER_ACCESSOR
            + FuncName
            + indexer.Parameters.ToString();
    }

    public string GetSelfName(Identifier identifier)
    {
        int SeparatorIndex = identifier.ResolvedName!.LastIndexOf(KGVL.NAMESPACE_SEPARATOR);
        if (SeparatorIndex == -1)
        {
            return identifier.ResolvedName;
        }

        // If the identifier ends with a separator, that is a programming bug and should crash the compiler.
        return identifier.ResolvedName[(SeparatorIndex + 1)..]; 
    }

    public string GetSemiResolvedIdentifier(PackMember member)
    {
        StringBuilder Builder = new();

        //Builder.Append(member.SelfIdentifier.SelfName);

        return Builder.ToString();
    }

    public string GetOperatorOverloadFunctionName(OperatorOverload overload, Identifier parentMemberIdentifier)
    {
        return parentMemberIdentifier.ResolvedName
            + KGVL.IDENTIFIER_OPERATOR
            + overload.OverloadedOperator.ToString()
            + KGVL.IDENTIFIER_SEPARATOR_FUNCTION
            + overload.Function.ToString();
    }
}
