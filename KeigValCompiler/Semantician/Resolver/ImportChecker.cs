namespace KeigValCompiler.Semantician.Resolver;

/* Checks that every "using" directive names a namespace which exists, which as in C# is one some file
 * declares, or one containing a namespace some file declares. */
internal class ImportChecker : IPackResolver
{
    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        HashSet<string> Existing = context.Pack.GetExistingNamespaceNames(null);
        foreach (PackSourceFile SourceFile in context.Pack.SourceFiles)
        {
            foreach (NamespaceImport Import in SourceFile.ImportDirectives)
            {
                string Name = Import.NameSpace.SelfIdentifier.SourceCodeName;
                if (!Existing.Contains(Name))
                {
                    context.AddError(context.ErrorCreator.UsingUnknownNamespace.CreateOptions(Name), SourceFile,
                        Import.Origin);
                }
            }
        }
    }
}
