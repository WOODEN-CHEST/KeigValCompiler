namespace KeigValCompiler.Semantician.Resolver;

internal class KGVLPackResolver : IPackResolver
{
    // Private methods.
    private void AddInternalContent(DataPack pack)
    {
        IInternalContentProvider ContentProvider = new KGVLInternalContentProvider();
        ContentProvider.AddInternalContent(pack);
    }

    private void ResolveNamespaceIdentifiers(DataPack pack)
    {
        foreach (PackNameSpace NameSpace in pack.NameSpaces)
        {
            NameSpace.SelfIdentifier.Target = NameSpace;
            NameSpace.SelfIdentifier.ResolvedName = NameSpace.SelfIdentifier.SourceCodeName;
            string Name = NameSpace.SelfIdentifier.SourceCodeName;
            NameSpace.SelfIdentifier.SelfName = Name.Substring(Name.IndexOf(KGVL.NAMESPACE_SEPARATOR) + 1);
        }
    }

    // Inherited methods.
    public void ResolvePack(DataPack pack)
    {
        // Order of method calls here must NOT be changed, or else this will incorrectly resolve things.
        ArgumentNullException.ThrowIfNull(pack, nameof(pack));
        AddInternalContent(pack);
        ResolveNamespaceIdentifiers(pack);
    }
}