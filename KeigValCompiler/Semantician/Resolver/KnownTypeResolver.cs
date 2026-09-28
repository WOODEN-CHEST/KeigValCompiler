using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Finds the type the standard library declares for each one the compiler knows by name. Only library
 * files are searched, so user code declaring a type of the same name in the same namespace cannot
 * take the place of the library's. This has to come before any type is looked up, because keywords
 * such as "int" resolve through what it finds, the library's own signatures included. */
internal class KnownTypeResolver : IPackResolver
{
    // Private methods.
    private PackMember? FindDeclaredType(LibraryType knownType, DataPack pack)
    {
        PackNameSpace? NameSpace = pack.TryGetNamespace(knownType.NameSpace);
        if (NameSpace == null)
        {
            return null;
        }

        return NameSpace.Types.FirstOrDefault(type => type.SourceFile.IsLibraryFile
            && (type.SelfIdentifier.SourceCodeName == knownType.Name)
            && (TypeDeclarationResolver.GetGenericParameterCount(type) == knownType.GenericParameterNames.Count));
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (LibraryType KnownType in LibraryTypes.All)
        {
            PackMember? DeclaredType = FindDeclaredType(KnownType, context.Pack);
            if (DeclaredType == null)
            {
                context.Messages.AddError(
                    context.ErrorCreator.LibraryTypeMissing.CreateOptions(KnownType.FullName),
                    CompilerMessageLocation.None, null);
                continue;
            }
            context.Registry.AddType(KnownType, DeclaredType);
        }
    }
}
