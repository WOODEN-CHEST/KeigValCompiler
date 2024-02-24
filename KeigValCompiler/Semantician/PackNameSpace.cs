using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackNameSpace
{
    // Internal fields.
    internal string Name { get; private init; }


    // Private fields.
    private readonly Dictionary<string, PackClass> _classes = new();
    private readonly Dictionary<string, PackFunction> _functions = new();
    private readonly Dictionary<string, PackField> _fields = new();
    private readonly Dictionary<string, PackProperty> _properties = new();


    // Constructors.
    internal PackNameSpace(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }


    // Methods.
    internal void AddClass(PackClass packClass)
    {
        if (_classes.ContainsKey(packClass.Name))
        {
            throw new PackContentException($"Pack {packClass.FullName} defined multiple times.");
        }
        _classes.Add(packClass.Name, packClass);
    }

    internal void AddFunction(PackFunction function)
    {
        if (_functions.ContainsKey(function.Name))
        {
            throw new PackContentException($"Pack {function.FullName} defined multiple times.");
        }
        _functions.Add(function.Name, function);
    }

    internal void AddField(PackField field)
    {
        if (_fields.ContainsKey(field.Name))
        {
            throw new PackContentException($"Pack {field.FullName} defined multiple times.");
        }
        _fields.Add(field.Name, field);
    }
    internal void AddProperty(PackProperty property)
    {
        if (_properties.ContainsKey(property.Name))
        {
            throw new PackContentException($"Pack {property.FullName} defined multiple times.");
        }
        _properties.Add(property.Name, property);
    }


    // Inherited methods.
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public override string ToString()
    {
        return Name;
    }

    public override bool Equals(object? obj)
    {
        if (obj is PackNameSpace)
        {
            return ((PackNameSpace)obj).Name == Name;
        }
        return false;
    }
}