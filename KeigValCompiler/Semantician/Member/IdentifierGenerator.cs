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
    /* The member's name within whatever holds it. Two members may share a name as long as their
     * generic parameter counts differ, as "Foo" and "Foo<T>" do, so the count is part of it, and so
     * is the interface an explicit implementation names. */
    public string GetFullResolvedIdentifier(PackMember member)
    {
        StringBuilder Builder = new((member.ParentItem != null)
            ? member.ParentItem.ResolvedName : member.NameSpace.SelfIdentifier.ResolvedName);
        Builder.Append(KGVL.NAMESPACE_SEPARATOR);

        if ((member is IExplicitInterfaceMember ExplicitMember) && (ExplicitMember.ExplicitInterface != null))
        {
            Builder.Append(GetTypeName(ExplicitMember.ExplicitInterface)).Append(KGVL.NAMESPACE_SEPARATOR);
        }
        Builder.Append(member.SelfIdentifier.SourceCodeName);

        if ((member is IGenericParameterHolder GenericsHolder) && (GenericsHolder.GenericParameters.Count > 0))
        {
            Builder.Append(KGVL.IDENTIFIER_GENERIC_ARITY).Append(GenericsHolder.GenericParameters.Count);
        }
        return Builder.ToString();
    }

    public string GetFullyResolvedFunctionIdentifier(PackFunction function)
    {
        return GetFullResolvedIdentifier(function)
            + KGVL.IDENTIFIER_SEPARATOR_FUNCTION
            + GetParameterTypeNames(function.Parameters);
    }

    /* A generic parameter belongs to the type or function declaring it, so its name is only unique
     * together with that one's. A function's is named before its parameters' types are known, so it
     * is given the function's name without them. */
    public string GetGenericParameterIdentifier(string ownerName, GenericTypeParameter parameter)
    {
        return ownerName
            + KGVL.IDENTIFIER_GENERIC_PARAMETER
            + parameter.SelfIdentifier.SourceCodeName;
    }

    /* An indexer's name, which unlike other members' has to carry its parameters, since a type can
     * have several indexers and they share the one name. */
    public string GetFullyResolvedIndexerIdentifier(PackIndexer indexer)
    {
        return GetFullResolvedIdentifier(indexer)
            + KGVL.IDENTIFIER_SEPARATOR_FUNCTION
            + GetParameterTypeNames(indexer.Parameters);
    }

    /* A type as its resolved names spell it, or as source code does where one is not resolved. */
    public string GetTypeName(TypeTargetIdentifier type)
    {
        return type.Format(identifier => identifier.ResolvedName ?? identifier.SourceCodeName);
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
            + FuncName;
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

    /* The return type is part of it, since two conversions from the same type differ in nothing
     * else. */
    public string GetOperatorOverloadFunctionName(OperatorOverload overload, Identifier parentMemberIdentifier)
    {
        StringBuilder Builder = new(parentMemberIdentifier.ResolvedName);
        TypeTargetIdentifier? ExplicitInterface = overload.Function.ExplicitInterface;
        if (ExplicitInterface != null)
        {
            Builder.Append(KGVL.NAMESPACE_SEPARATOR).Append(GetTypeName(ExplicitInterface));
        }

        Builder.Append(KGVL.IDENTIFIER_OPERATOR)
            .Append(overload.OverloadedOperator.ToString())
            .Append(KGVL.IDENTIFIER_SEPARATOR_FUNCTION)
            .Append(GetParameterTypeNames(overload.Function.Parameters));

        if (overload.Function.ReturnType != null)
        {
            Builder.Append(KGVL.IDENTIFIER_SEPARATOR_FUNCTION).Append(GetTypeName(overload.Function.ReturnType));
        }
        return Builder.ToString();
    }


    // Private methods.
    /* Parameters differ by type and by ref, out, in or params, never by name. */
    private string GetParameterTypeNames(FunctionParameterCollection parameters)
    {
        return string.Join(KGVL.COMMA, parameters.Select(GetParameterTypeName));
    }

    /* A lambda's parameter may have no type written, which leaves only its modifier. */
    private string GetParameterTypeName(FunctionParameter parameter)
    {
        string TypeName = (parameter.Type != null) ? GetTypeName(parameter.Type) : string.Empty;
        return (parameter.Modifiers == FunctionParameterModifier.None)
            ? TypeName : parameter.Modifiers.ToString() + KGVL.IDENTIFIER_SEPARATOR_FUNCTION + TypeName;
    }
}
