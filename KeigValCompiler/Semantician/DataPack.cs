using KeigValCompiler.Semantician.Member;
using System.Xml.Linq;

namespace KeigValCompiler.Semantician;

internal class DataPack
{
    // Internal fields.
    internal IEnumerable<PackSourceFile> SourceFiles => _sourceFiles;
    internal IEnumerable<PackNameSpace> NameSpaces => _sourceFiles.SelectMany(file => file.AllUsedNamespaces).Distinct();
    public IEnumerable<PackClass> Classes => NameSpaces.SelectMany(nameSpace => nameSpace.AllClasses);
    public IEnumerable<PackInterface> Interfaces => NameSpaces.SelectMany(nameSpace => nameSpace.AllInterfaces);
    public IEnumerable<PackStruct> Structs => NameSpaces.SelectMany(nameSpace => nameSpace.AllStructs);
    public IEnumerable<PackProperty> Properties => NameSpaces.SelectMany(nameSpace => nameSpace.AllProperties);
    public IEnumerable<PackField> Fields => NameSpaces.SelectMany(nameSpace => nameSpace.AllFields);
    public IEnumerable<PackFunction> Functions => NameSpaces.SelectMany(nameSpace => nameSpace.AllFunctions);
    public IEnumerable<PackEnumeration> Enums => NameSpaces.SelectMany(nameSpace => nameSpace.AllEnums);
    public IEnumerable<PackIndexer> Indexers => NameSpaces.SelectMany(nameSpace => nameSpace.AllIndexers);
    public IEnumerable<PackDelegate> Delegates => NameSpaces.SelectMany(nameSpace => nameSpace.AllDelegates);
    public IEnumerable<PackEvent> Events => NameSpaces.SelectMany(nameSpace => nameSpace.AllEvents);
    public IEnumerable<PackMember> Members => NameSpaces.SelectMany(nameSpace => nameSpace.AllMembers);
    public IEnumerable<PackMember> Types => NameSpaces.SelectMany(nameSpace => nameSpace.AllTypes);


    // Private fields.
    private List<PackSourceFile> _sourceFiles = new();


    // Methods.
    public void AddSourceFile(PackSourceFile sourceFile)
    {
        _sourceFiles.Add(sourceFile ?? throw new ArgumentNullException(nameof(sourceFile)));
    }

    /* The full name of every namespace which exists, which as in C# is one some file declares, or one
     * containing a namespace some file declares: "KGVL.Collections" exists because "KGVL.Collections.Generic"
     * does. A namespace only named by a "using" directive does not exist by that. Given a kind of file, only
     * the namespaces files of that kind declare are counted, as a C# assembly counts only its own. */
    internal HashSet<string> GetExistingNamespaceNames(SourceFileKind? kind)
    {
        HashSet<string> Existing = new();
        foreach (PackSourceFile SourceFile in _sourceFiles.Where(file => (kind == null) || (file.Kind == kind)))
        {
            foreach (PackNameSpace NameSpace in SourceFile.Namespaces)
            {
                string? Name = NameSpace.SelfIdentifier.SourceCodeName;
                while ((Name != null) && Existing.Add(Name))
                {
                    int SeparatorIndex = Name.LastIndexOf(KGVL.NAMESPACE_SEPARATOR);
                    Name = (SeparatorIndex == -1) ? null : Name[..SeparatorIndex];
                }
            }
        }
        return Existing;
    }

    public PackNameSpace? TryGetNamespace(string fullName)
    {
        foreach (PackNameSpace NameSpace in NameSpaces)
        {
            if ((NameSpace.SelfIdentifier.ResolvedName ?? NameSpace.SelfIdentifier.SourceCodeName) == fullName)
            {
                return NameSpace;
            }
        }
        return null;
    }
}