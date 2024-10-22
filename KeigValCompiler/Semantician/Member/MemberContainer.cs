
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal class MemberContainer : IPackTypeHolder, IPackFieldHolder, IPackFunctionHolder
{
    // Fields.
    public OperatorOverloadCollection OperatorOverloads { get; private init; } = new();
    public GenericTypeParameterCollection GenericParameters { get; private init; } = new();
    public IIdentifiable[] ExtendedMembers => _extendedItems.ToArray();
    public PackClass[] Classes => _classes.ToArray();
    public PackInterface[] Interfaces => _interfaces.ToArray();
    public PackStruct[] Structs => _structs.ToArray();
    public Identifier[] ExtendedItems { get; private init; }
    public PackProperty[] Properties => _properties.ToArray();
    public PackField[] Fields => _fields.ToArray();
    public PackFunction[] Functions => _functions.ToArray();
    public PackEnumeration[] Enums => _enums.ToArray();
    public PackIndexer[] Indexers => _indexers.ToArray();


    // Private fields.
    private readonly List<IIdentifiable> _extendedItems = new();
    private readonly List<PackFunction> _functions = new();
    private readonly List<PackProperty> _properties = new();
    private readonly List<PackField> _fields = new();
    private readonly List<PackIndexer> _indexers = new();
    private readonly List<PackClass> _classes = new();
    private readonly List<PackInterface> _interfaces = new();
    private readonly List<PackEnumeration> _enums = new();
    private readonly List<PackStruct> _structs = new();


    // Inherited methods.
    public void AddClass(PackClass item)
    {
        _classes.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void AddInterface(PackInterface item)
    {
        _interfaces.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void AddProperty(PackProperty item)
    {
        _properties.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void AddField(PackField item)
    {
        _fields.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void AddFunction(PackFunction item)
    {
        _functions.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void AddEnum(PackEnumeration item)
    {
        _enums.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void AddStruct(PackStruct item)
    {
        _structs.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void AddIndexer(PackIndexer item)
    {
        _indexers.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void RemoveClass(PackClass item)
    {
        _classes.Remove(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void RemoveInterface(PackInterface item)
    {
        _interfaces.Remove(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void RemoveProperty(PackProperty item)
    {
        _properties.Remove(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void RemoveField(PackField item)
    {
        _fields.Remove(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void RemoveFunction(PackFunction item)
    {
        _functions.Remove(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void RemoveEnum(PackEnumeration item)
    {
        _enums.Remove(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void RemoveStruct(PackStruct item)
    {
        _structs.Remove(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void RemoveIndexer(PackIndexer item)
    {
        _indexers.Remove(item ?? throw new ArgumentNullException(nameof(item)));
    }

    public void AddExtendedMember(IIdentifiable identifier)
    {
        _extendedItems.Add(identifier ?? throw new ArgumentNullException(nameof(identifier)));
    }

    public void RemoveExtendedMember(IIdentifiable identifier)
    {
        _extendedItems.Remove(identifier ?? throw new ArgumentNullException(nameof(identifier)));
    }
}