namespace KeigValCompiler.Semantician.Member;

internal class PackInterface : PackMember, IPackTypeHolder, IPackFunctionHolder,
    IPackMemberExtender, IOperatorOverloadHolder, IGenericParameterHolder, IPackFieldHolder, IPackType,
    IPackEventHolder
{
    // Fields.
    public OperatorOverloadCollection OperatorOverloads => _members.OperatorOverloads;
    public GenericTypeParameterCollection GenericParameters { get; private init; } = new();
    public IEnumerable<TypeTargetIdentifier> ExtendedMembers => _extendedMembers.ToArray();
    public IEnumerable<PackClass> Classes => _members.Classes;
    public IEnumerable<PackInterface> Interfaces => _members.Interfaces;
    public IEnumerable<PackStruct> Structs => _members.Structs;
    public IEnumerable<PackProperty> Properties => _members.Properties;
    public IEnumerable<PackField> Fields => _members.Fields;
    public IEnumerable<PackFunction> Functions => _members.Functions;
    public IEnumerable<PackEnumeration> Enums => _members.Enums;
    public IEnumerable<PackIndexer> Indexers => _members.Indexers;
    public IEnumerable<PackDelegate> Delegates => _members.Delegates;
    public IEnumerable<PackFunction> Constructors => Functions.Where(
        function => function.SelfIdentifier.SourceCodeName == SelfIdentifier.SelfName).ToArray();
    public IEnumerable<PackMember> Types => _members.Types;
    public IEnumerable<PackMember> AllTypes => _members.AllTypes;
    public IEnumerable<PackEvent> Events => _members.Events;
    public IEnumerable<PackClass> AllClasses => _members.AllClasses;
    public IEnumerable<PackInterface> AllInterfaces => _members.AllInterfaces;
    public IEnumerable<PackStruct> AllStructs => _members.AllStructs;
    public IEnumerable<PackEnumeration> AllEnums => _members.AllEnums;
    public IEnumerable<PackDelegate> AllDelegates => _members.AllDelegates;
    public IEnumerable<PackField> AllFields => _members.AllFields;
    public IEnumerable<PackFunction> AllFunctions => _members.AllFunctions;
    public IEnumerable<PackProperty> AllProperties => _members.AllProperties;
    public IEnumerable<PackIndexer> AllIndexers => _members.AllIndexers;
    public IEnumerable<PackEvent> AllEvents => _members.AllEvents;
    public int ExtendedMemberCount => _extendedMembers.Count;


    // Internal fields.
    internal override IEnumerable<PackMember> SubMembers => _members.Members
        .Concat(OperatorOverloads.Select(overload => overload.Function));
    internal override IEnumerable<PackMember> AllSubMembers => _members.AllMembers;


    // Private fields.
    private readonly List<TypeTargetIdentifier> _extendedMembers = new();
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

    public void AddDelegate(PackDelegate packDelegate)
    {
        _members.AddDelegate(packDelegate);
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

    public void AddExtendedMember(TypeTargetIdentifier type)
    {
        _extendedMembers.Add(type);
    }

    public void RemoveExtendedMember(TypeTargetIdentifier type)
    {
        _extendedMembers.Remove(type);
    }

    public void RemoveDelegate(PackDelegate packDelegate)
    {
        _members.RemoveDelegate(packDelegate);
    }

    public void AddEvent(PackEvent packEvent)
    {
        _members.AddEvent(packEvent);
    }

    public void RemoveEvent(PackEvent packEvent)
    {
        _members.RemoveEvent(packEvent);
    }
}