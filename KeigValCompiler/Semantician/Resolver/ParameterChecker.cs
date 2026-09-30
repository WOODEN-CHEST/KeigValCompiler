using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks each parameter list against C#'s rules for what may follow what: a "params" parameter comes last,
 * and a parameter with a default value, which makes it optional, is followed only by other optional ones and
 * a "params" one. A "params" parameter's type is an array, which the arguments given for it are gathered
 * into. A parameter passed by "ref" or "out", or a "params" one, cannot have a default value, and a default
 * a member can never use, since every call to it gives each argument, is warned about. Whether a default
 * value is a constant of the parameter's type waits for expressions to be resolved. Each mistake is
 * reported once: a default which cannot be had does not also make what follows it misplaced, unlike in
 * Roslyn, and an operator's "ref", "out" and "params" are OperatorDeclarationChecker's to report. */
internal class ParameterChecker : IPackResolver
{
    // Private methods.
    private void CheckMember(PackMember member, PackResolutionContext context)
    {
        FunctionParameterCollection? Parameters = MemberRelations.GetParameters(member);
        if ((Parameters == null) || (Parameters.Count == 0))
        {
            return;
        }

        bool IsOperator = MemberRelations.GetOperatorOverload(member) != null;
        string Kind = MemberRelations.GetKindName(member);
        string Name = MemberRelations.GetDisplayName(member);
        WarningDefinition? UnusableDefault = GetUnusableDefaultWarning(member, Parameters.Count, IsOperator,
            context);
        FunctionParameter? FirstOptional = null;
        FunctionParameter Last = Parameters.Last();

        foreach (FunctionParameter Parameter in Parameters)
        {
            string ParameterName = Parameter.SelfIdentifier.SourceCodeName;
            bool IsParams = (Parameter.Modifiers & FunctionParameterModifier.Params)
                != FunctionParameterModifier.None;
            if (IsParams && !IsOperator && !IsArrayOrUnresolved(Parameter, context))
            {
                context.AddError(context.ErrorCreator.ParamsNotArray.CreateOptions(ParameterName, Kind, Name,
                    Parameter.Type!.ToString()), member);
            }
            if (IsParams && !IsOperator && !ReferenceEquals(Parameter, Last))
            {
                context.AddError(context.ErrorCreator.ParamsNotLast.CreateOptions(ParameterName, Kind, Name),
                    member);
                if (Parameter.DefaultValue != null)
                {
                    ReportDisallowedDefault(Parameter, IsParams, Kind, Name, member, context);
                }
                continue;
            }

            if (Parameter.DefaultValue == null)
            {
                if ((FirstOptional != null) && !IsParams)
                {
                    context.AddError(context.ErrorCreator.RequiredAfterOptional.CreateOptions(ParameterName, Kind,
                        Name, FirstOptional.SelfIdentifier.SourceCodeName), member);
                }
                continue;
            }

            if (!IsOperator && ReportDisallowedDefault(Parameter, IsParams, Kind, Name, member, context))
            {
                continue;
            }
            FirstOptional ??= Parameter;
            if (UnusableDefault != null)
            {
                context.AddWarning(UnusableDefault.CreateOptions(ParameterName, Kind, Name), member);
            }
        }
    }

    /* Whether a parameter's type is an array, written as "int[]" or as "Array<int>". One whose type did not
     * resolve has been reported, and is taken to be one. */
    private bool IsArrayOrUnresolved(FunctionParameter parameter, PackResolutionContext context)
    {
        if (parameter.Type == null)
        {
            return true;
        }
        SemanticType? Type = context.TypeReader.Read(parameter.Type);
        return (Type == null) || ((Type is DeclaredType Declared) && (Declared.LibraryType == LibraryTypes.Array));
    }

    /* Reports a default value the parameter cannot have, and says whether it did. */
    private bool ReportDisallowedDefault(FunctionParameter parameter,
        bool isParams,
        string kind,
        string name,
        PackMember member,
        PackResolutionContext context)
    {
        string ParameterName = parameter.SelfIdentifier.SourceCodeName;
        string? ReferenceKeyword = (parameter.Modifiers & (FunctionParameterModifier.Ref
            | FunctionParameterModifier.Out)) switch
        {
            FunctionParameterModifier.Ref => KGVL.KEYWORD_REF,
            FunctionParameterModifier.Out => KGVL.KEYWORD_OUT,
            _ => null
        };

        if (ReferenceKeyword != null)
        {
            context.AddError(context.ErrorCreator.DefaultValueByReference.CreateOptions(ParameterName, kind, name,
                ReferenceKeyword), member);
            return true;
        }
        if (isParams)
        {
            context.AddError(context.ErrorCreator.DefaultValueOnParams.CreateOptions(ParameterName, kind, name),
                member);
            return true;
        }
        return false;
    }

    /* The warning C# gives for a member whose default values can never be used, since every call gives each
     * argument: an explicit implementation, called only through its interface, an operator, and an indexer of
     * one parameter. Null for any other member. */
    private WarningDefinition? GetUnusableDefaultWarning(PackMember member,
        int parameterCount,
        bool isOperator,
        PackResolutionContext context)
    {
        if (MemberRelations.GetExplicitInterface(member) != null)
        {
            return context.ErrorCreator.UnusableDefaultOnExplicitImplementation;
        }
        if (isOperator)
        {
            return context.ErrorCreator.UnusableDefaultOnOperator;
        }
        return ((member is PackIndexer) && (parameterCount == 1))
            ? context.ErrorCreator.UnusableDefaultOnSingleIndex : null;
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackMember Member in context.Pack.Members)
        {
            CheckMember(Member, context);
        }
    }
}
