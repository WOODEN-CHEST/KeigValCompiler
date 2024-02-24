using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackClass
{
    // Internal fields.
    /* Persistent info (always available). */
    internal string NameSpace { get; private init; }
    internal string Name { get; private init; }
    internal string FullName { get; private init; }
    internal PackClass? ParentClass { get; private init; }
    internal PackMemberModifiers Modifiers { get; private init; }
    internal bool IsStatic => (Modifiers & PackMemberModifiers.Static) != 0;
    internal bool IsAbstract => (Modifiers & PackMemberModifiers.Abstract) != 0;


    /* Resolved info. (Available only after info-building stage). */
    internal PackClass? BaseClass { get; private set; } = null;
    internal PackInterface[] ImplementedInterfaces { get; private set; } = Array.Empty<PackInterface>();


    // Private fields.
    /* Early info (always available, but only used before info-building stage). */
    private readonly string[] _extendedItems;

    


    // Private fields.
    private readonly Dictionary<string, PackFunction> _functions = new();
    private readonly Dictionary<string, PackField> _fields = new();


    // Constructors.
    internal PackClass(string nameSpace, 
        string name, 
        PackClass? parentClass, 
        PackMemberModifiers modifiers,
        string[] extendedItems
        )
    {
        NameSpace = nameSpace ?? throw new ArgumentNullException(nameof(nameSpace));
        Name = name ?? throw new ArgumentNullException(nameof(nameSpace));
        FullName = $"{NameSpace}.{Name}";
        ParentClass = parentClass;
        Modifiers = modifiers;
        _extendedItems = extendedItems ?? throw new ArgumentNullException(nameof(extendedItems));
    }


    // Internal methods.
    /* Info-building stage. */
    internal void BuildInfo(DataPack pack)
    {

    }

    internal void AddFunction(PackFunction function) => _functions.Add(function.Name, function);

    internal void AddField(PackField field) => _fields.Add(field.Name, field);



    // Inherited methods.
    public override int GetHashCode()
    {
        return FullName.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is PackClass)
        {
            return FullName == ((PackClass)obj).FullName;
        }
        return false;
    }

    public override string ToString()
    {
        return FullName;
    }
}