namespace KeigValCompiler.Semantician.Member;

internal class PackInterface : PackMember, IPackTypeHolder, IPackFunctionHolder,
    IPackMemberExtender, IGenericParameterHolder, IPackFieldHolder
{
    // Fields.
    public GenericTypeParameterCollection GenericParameters { get; private init; } = new();
    public Identifier[] ExtendedMembers => _extendedMember.ToArray();
    public PackClass[] Classes => _members.Classes;
    public PackInterface[] Interfaces => _members.Interfaces;
    public PackStruct[] Structs => _members.Structs;
    public PackProperty[] Properties => _members.Properties;
    public PackField[] Fields => _members.Fields;
    public PackFunction[] Functions => _members.Functions;
    public PackEnumeration[] Enums => _members.Enums;
    public PackIndexer[] Indexers => _members.Indexers;


    // Private fields.
    private readonly List<Identifier> _extendedMember = new();
    private readonly MemberContainer _members = new();


    // Constructors.
    internal PackInterface(Identifier identifier, PackSourceFile sourceFile) : base(identifier, sourceFile) { }


    // Inherited methods.
    public void AddClass(PackClass item)
    {
        _members.AddClass(item);
    }

    public void AddInterface(PackInterface item)
    {
        _members.AddInterface(item);
    }

    public void AddProperty(PackProperty item)
    {
        _members.AddProperty(item);
    }

    public void AddField(PackField item)
    {
        _members.AddField(item);
    }

    public void AddFunction(PackFunction item)
    {
        _members.AddFunction(item);
    }

    public void AddEnum(PackEnumeration item)
    {
        _members.AddEnum(item);
    }

    public void AddStruct(PackStruct item)
    {
        _members.AddStruct(item);
    }

    public void AddIndexer(PackIndexer item)
    {
        _members.AddIndexer(item);
    }

    public void RemoveClass(PackClass item)
    {
        _members.RemoveClass(item);
    }

    public void RemoveInterface(PackInterface item)
    {
        _members.RemoveInterface(item);
    }

    public void RemoveProperty(PackProperty item)
    {
        _members.RemoveProperty(item);
    }

    public void RemoveField(PackField item)
    {
        _members.RemoveField(item);
    }

    public void RemoveFunction(PackFunction item)
    {
        _members.RemoveFunction(item);
    }

    public void RemoveEnum(PackEnumeration item)
    {
        _members.RemoveEnum(item);
    }

    public void RemoveStruct(PackStruct item)
    {
        _members.RemoveStruct(item);
    }

    public void RemoveIndexer(PackIndexer item)
    {
        _members.RemoveIndexer(item);
    }

    public void AddExtendedMember(Identifier identifier)
    {
        _extendedMember.Add(identifier);
    }

    public void RemoveExtendedMember(Identifier identifier)
    {
        _extendedMember.Remove(identifier);
    }
}