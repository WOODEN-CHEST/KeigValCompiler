using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Names every type, and every type's generic parameters, before any type is looked up, since a type
 * may be used above its declaration and in other files. Two types of the same name and generic
 * parameter count in the same namespace or type are reported, whichever files they are in. */
internal class TypeDeclarationResolver : IPackResolver
{
    // Internal static methods.
    internal static int GetGenericParameterCount(PackMember member)
    {
        return (member as IGenericParameterHolder)?.GenericParameters.Count ?? 0;
    }


    // Private methods.
    /* An outer type is named before the types inside it, whose names are built on its own. */
    private void ResolveTypes(IEnumerable<PackMember> types, PackResolutionContext context)
    {
        Dictionary<(string Name, int GenericParameterCount), PackMember> DeclaredTypes = new();

        foreach (PackMember Type in types)
        {
            Type.SelfIdentifier.ResolvedName = context.IdentifierGenerator.GetFullResolvedIdentifier(Type);
            Type.SelfIdentifier.SelfName = Type.SelfIdentifier.SourceCodeName;
            Type.SelfIdentifier.Target = Type;
            ResolveGenericParameterNames(Type, context);

            (string, int) Key = (Type.SelfIdentifier.SourceCodeName, GetGenericParameterCount(Type));
            if (DeclaredTypes.TryGetValue(Key, out PackMember? EarlierType))
            {
                context.AddError(
                    context.ErrorCreator.DuplicateType.CreateOptions(Type.SelfIdentifier.ResolvedName), Type, $"It is also declared in \"{EarlierType.SourceFile.Path}\", on line "
                        + $"{EarlierType.SourceFileOrigin.Line}");
            }
            else
            {
                DeclaredTypes.Add(Key, Type);
            }

            if (Type is IPackTypeHolder TypeHolder)
            {
                ResolveTypes(TypeHolder.Types, context);
            }
        }
    }

    private void ResolveGenericParameterNames(PackMember type, PackResolutionContext context)
    {
        if (type is not IGenericParameterHolder GenericsHolder)
        {
            return;
        }

        foreach (GenericTypeParameter Parameter in GenericsHolder.GenericParameters)
        {
            Parameter.SelfIdentifier.ResolvedName = context.IdentifierGenerator
                .GetGenericParameterIdentifier(type.SelfIdentifier.ResolvedName!, Parameter);
            Parameter.SelfIdentifier.SelfName = Parameter.SelfIdentifier.SourceCodeName;
            Parameter.SelfIdentifier.Target = Parameter;
        }
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackNameSpace NameSpace in context.Pack.NameSpaces)
        {
            ResolveTypes(NameSpace.Types, context);
        }
    }
}
