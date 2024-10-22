using KeigValCompiler.Semantician.Member;
using System.Collections;

namespace KeigValCompiler.Semantician;

internal class PackNameSpace : IPackTypeHolder, IPackFieldHolder, IPackFunctionHolder, IIdentifiable
{
    // Fields.
    public PackClass[] Classes => _members.Classes;
    public PackInterface[] Interfaces => _members.Interfaces;
    public PackStruct[] Structs => _members.Structs;
    public PackProperty[] Properties => _members.Properties;
    public PackField[] Fields => _members.Fields;
    public PackFunction[] Functions => _members.Functions;
    public PackEnumeration[] Enums => _members.Enums;
    public PackIndexer[] Indexers => _members.Indexers;
    public PackMember[] Types => Enumerable.Empty<PackMember>().Concat(Classes)
        .Concat(Interfaces).Concat(Structs).Concat(Enums).ToArray();

    public PackClass[] AllClasses => AddClassesFromTypeHolder(new(), this).ToArray();
    public PackInterface[] AllInterfaces => AddInterfacesFromTypeHolder(new(), this).ToArray();
    public PackStruct[] AllStructs => AddStructsFromTypeHolder(new(), this).ToArray();
    public PackProperty[] AllProperties => GetAllTypeHolders<IPackFunctionHolder>().SelectMany(holder => holder.Properties).ToArray();
    public PackField[] AllFields => GetAllTypeHolders<IPackFieldHolder>().SelectMany(holder => holder.Fields).ToArray();
    public PackFunction[] AllFunctions => GetAllTypeHolders<IPackFunctionHolder>().SelectMany(holder => holder.Functions).ToArray();
    public PackEnumeration[] AllEnums => GetAllTypeHolders<IPackTypeHolder>().SelectMany(holder => holder.Enums).ToArray();
    public PackIndexer[] AllIndexers => GetAllTypeHolders<IPackFunctionHolder>().SelectMany(holder => holder.Indexers).ToArray();
    public PackMember[] AllTypes => Enumerable.Empty<PackMember>().Concat(AllClasses)
        .Concat(AllInterfaces).Concat(AllStructs).Concat(AllEnums).ToArray();
    public PackMember[] AllMembers => Enumerable.Empty<PackMember>().Concat(AllClasses).Concat(AllInterfaces).Concat(AllStructs)
        .Concat(AllProperties).Concat(AllFields).Concat(AllFunctions).Concat(AllEnums).Concat(AllIndexers).ToArray();

    public Identifier SelfIdentifier { get; private init; }


    // Private fields.
    private readonly MemberContainer _members = new();


    // Constructors.
    internal PackNameSpace(Identifier identifier)
    {
        SelfIdentifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
    }


    // Private methods.
    private List<PackClass> AddClassesFromTypeHolder(List<PackClass> items, IPackTypeHolder holder)
    {
        items.AddRange(holder.Classes);
        foreach (PackClass Target in holder.Classes)
        {
            AddClassesFromTypeHolder(items, Target);
        }
        return items;
    }

    private List<PackInterface> AddInterfacesFromTypeHolder(List<PackInterface> items, IPackTypeHolder holder)
    {
        items.AddRange(holder.Interfaces);
        foreach (PackInterface target in holder.Interfaces)
        {
            AddInterfacesFromTypeHolder(items, target);
        }
        return items;
    }

    private List<PackStruct> AddStructsFromTypeHolder(List<PackStruct> items, IPackTypeHolder holder)
    {
        items.AddRange(holder.Structs);
        foreach (PackStruct target in holder.Structs)
        {
            AddStructsFromTypeHolder(items, target);
        }
        return items;
    }

    private IEnumerable<T> GetAllTypeHolders<T>()
    {
        return Enumerable.Empty<IPackTypeHolder>().Concat(AllInterfaces).Concat(AllStructs)
            .Concat(AllInterfaces).Select(item => (T)item);
    }


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
}