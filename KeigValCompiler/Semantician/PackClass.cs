using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackClass : PackMember
{
    // Internal fields.
    internal PackClass? BaseClass { get; private set; } = null;
    internal PackInterface[] ImplementedInterfaces { get; private set; } = Array.Empty<PackInterface>();


    // Private fields.
    private readonly string[] _extensions;

    


    // Private fields.
    private readonly Dictionary<string, PackFunction> _functions = new();
    private readonly Dictionary<string, PackField> _fields = new();


    // Constructors.
    internal PackClass(string name, PackClass parentClass, PackMemberModifiers modifiers, string[] extendedItems)
        : base(name, parentClass, modifiers)
    {
        ParentClass = parentClass;
        _extensions = extendedItems ?? throw new ArgumentNullException(nameof(extendedItems));
    }

    internal PackClass(string name, string parentNamespace, PackMemberModifiers modifiers, string[] extendedItems, PackSourceFile sourceFile)
        : base(name, parentNamespace, modifiers, sourceFile)
    {
        _extensions = extendedItems ?? throw new ArgumentNullException(nameof(extendedItems));
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