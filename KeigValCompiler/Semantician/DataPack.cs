using KeigValCompiler.Semantician.Member;
using System.Xml.Linq;

namespace KeigValCompiler.Semantician;

internal class DataPack
{
    // Internal fields.
    internal IEnumerable<PackSourceFile> SourceFiles => _sourceFiles;
    internal IEnumerable<PackNameSpace> NameSpaces => _sourceFiles.SelectMany(file => file.Namespaces).Distinct();

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
}