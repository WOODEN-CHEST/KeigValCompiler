using KeigValCompiler.Semantician.Member;
using System.Xml.Linq;

namespace KeigValCompiler.Semantician;

internal class DataPack
{
    // Internal fields.
    internal PackSourceFile[] SourceFiles => _sourceFiles.ToArray();
    internal PackNameSpace[] NameSpaces => _sourceFiles.SelectMany(file => file.Namespaces).Distinct().ToArray();

    public PackClass[] Classes => NameSpaces.SelectMany(nameSpace => nameSpace.AllClasses).ToArray();
    public PackInterface[] Interfaces => NameSpaces.SelectMany(nameSpace => nameSpace.AllInterfaces).ToArray();
    public PackStruct[] Structs => NameSpaces.SelectMany(nameSpace => nameSpace.AllStructs).ToArray();
    public PackProperty[] Properties => NameSpaces.SelectMany(nameSpace => nameSpace.AllProperties).ToArray();
    public PackField[] Fields => NameSpaces.SelectMany(nameSpace => nameSpace.AllFields).ToArray();
    public PackFunction[] Functions => NameSpaces.SelectMany(nameSpace => nameSpace.AllFunctions).ToArray();
    public PackEnumeration[] Enums => NameSpaces.SelectMany(nameSpace => nameSpace.AllEnums).ToArray();
    public PackIndexer[] Indexers => NameSpaces.SelectMany(nameSpace => nameSpace.AllIndexers).ToArray();
    public PackMember[] Members => NameSpaces.SelectMany(nameSpace => nameSpace.AllMembers).ToArray();
    public PackMember[] Types => NameSpaces.SelectMany(nameSpace => nameSpace.AllTypes).ToArray();


    // Private fields.
    private List<PackSourceFile> _sourceFiles = new();


    // Methods.
    public void AddSourceFile(PackSourceFile sourceFile)
    {
        _sourceFiles.Add(sourceFile ?? throw new ArgumentNullException(nameof(sourceFile)));
    }
}