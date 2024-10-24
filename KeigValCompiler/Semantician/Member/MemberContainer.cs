using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

// The code for AllX properties is made based on current requirements, changes in them may require lots of changes to that code, 
// since it makes assumptions about what capabilities certain classes have.
internal class MemberContainer : IPackTypeHolder, IPackFieldHolder, IPackFunctionHolder, IPackEventHolder, IOperatorOverloadHolder
{
    // Fields.
    public IEnumerable<PackClass> Classes => _classes;
    public IEnumerable<PackInterface> Interfaces => _interfaces;
    public IEnumerable<PackStruct> Structs => _structs;
    public IEnumerable<PackProperty> Properties => _properties;
    public IEnumerable<PackField> Fields => _fields;
    public IEnumerable<PackFunction> Functions => _functions;
    public IEnumerable<PackEnumeration> Enums => _enums;
    public IEnumerable<PackIndexer> Indexers => _indexers;
    public IEnumerable<PackDelegate> Delegates => _delegates;
    public IEnumerable<PackEvent> Events => _events;

    public IEnumerable<PackMember> Types => Enumerable.Empty<PackMember>().Concat(Classes)
        .Concat(Interfaces).Concat(Enums).Concat(Structs).Concat(Delegates);

    public IEnumerable<PackMember> AllTypes => Types.Concat(GetTypeHolders().SelectMany(
        holder => holder.AllTypes));

    public IEnumerable<PackClass> AllClasses => Classes.Concat(GetTypeHolders().SelectMany(
        holder => holder.AllClasses));

    public IEnumerable<PackInterface> AllInterfaces => Interfaces.Concat(GetTypeHolders().SelectMany(
        holder => holder.AllInterfaces));

    public IEnumerable<PackStruct> AllStructs => Structs.Concat(GetTypeHolders().SelectMany(
        holder => holder.AllStructs));

    public IEnumerable<PackEnumeration> AllEnums => Enums.Concat(GetTypeHolders().SelectMany(
        holder => holder.AllEnums));

    public IEnumerable<PackDelegate> AllDelegates => Delegates.Concat(GetTypeHolders().SelectMany(
        holder => holder.AllDelegates));

    public IEnumerable<PackField> AllFields => Fields.Concat(GetTypeHolders().SelectMany(
        holder => ((IPackFieldHolder)holder).AllFields));

    public IEnumerable<PackFunction> AllFunctions => Functions.Concat(GetTypeHolders().SelectMany(
        holder => ((IPackFunctionHolder)holder).AllFunctions)).Concat(OperatorOverloads.Select(overload => overload.Function));

    public IEnumerable<PackProperty> AllProperties => Properties.Concat(GetTypeHolders().SelectMany(
        holder => ((IPackFunctionHolder)holder).AllProperties));

    public IEnumerable<PackIndexer> AllIndexers => Indexers.Concat(GetTypeHolders().SelectMany(
        holder => ((IPackFunctionHolder)holder).AllIndexers));

    public IEnumerable<PackEvent> AllEvents => Events.Concat(GetTypeHolders().SelectMany(
        holder => ((IPackEventHolder)holder).AllEvents));

    public IEnumerable<PackMember> Members => Enumerable.Empty<PackMember>().Concat(Classes).Concat(Interfaces).Concat(Structs)
        .Concat(Delegates).Concat(Events).Concat(Fields).Concat(Functions).Concat(Properties).Concat(Indexers);
    public IEnumerable<PackMember> AllMembers => Members.Concat(Members.SelectMany(member => member.AllSubMembers))
        .Concat(OperatorOverloads.Select(overload => overload.Function));

    public OperatorOverloadCollection OperatorOverloads { get; } = new();


    // Private fields.
    private readonly List<PackFunction> _functions = new();
    private readonly List<PackProperty> _properties = new();
    private readonly List<PackField> _fields = new();
    private readonly List<PackIndexer> _indexers = new();
    private readonly List<PackClass> _classes = new();
    private readonly List<PackInterface> _interfaces = new();
    private readonly List<PackEnumeration> _enums = new();
    private readonly List<PackStruct> _structs = new();
    private readonly List<PackDelegate> _delegates = new();
    private readonly List<PackEvent> _events = new();


    // Private methods.
    private IEnumerable<IPackTypeHolder> GetTypeHolders()
    {
        return Enumerable.Empty<IPackTypeHolder>().Concat(Classes).Concat(Interfaces).Concat(Structs);
    }


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

    public void AddEvent(PackEvent packEvent)
    {
        _events.Add(packEvent ?? throw new ArgumentNullException(nameof(packEvent)));
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

    public void AddDelegate(PackDelegate packDelegate)
    {
        _delegates.Add(packDelegate ?? throw new ArgumentNullException(nameof(packDelegate)));
    }

    public void RemoveDelegate(PackDelegate packDelegate)
    {
        _delegates.Remove(packDelegate ?? throw new ArgumentNullException(nameof(packDelegate)));
    }

    public void RemoveEvent(PackEvent packEvent)
    {
        _events.Remove(packEvent ?? throw new ArgumentNullException(nameof(packEvent)));
    }
}