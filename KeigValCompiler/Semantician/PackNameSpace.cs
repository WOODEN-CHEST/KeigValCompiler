using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician;

internal class PackNameSpace
{
    // Internal fields.
    internal string Name { get; private init; }
    internal PackClass[] Classes => _classes.Values.ToArray();
    internal PackFunction[] Functions => _functions.Values.ToArray();
    internal PackField[] Fields => _fields.Values.ToArray();
    internal PackProperty[] Properties => _properties.Values.ToArray();



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
        if (_classes.ContainsKey(packClass.Identifier))
        {
            throw new PackContentException($"Pack {packClass.FullyQualifiedIdentifier} defined multiple times.");
        }
        _classes.Add(packClass.Identifier, packClass);
    }

    internal void AddFunction(PackFunction function)
    {
        if (_functions.ContainsKey(function.FullyQualifiedIdentifier))
        {
            throw new PackContentException($"Pack {function.FullyQualifiedIdentifier} defined multiple times.");
        }
        _functions.Add(function.FullyQualifiedIdentifier, function);
    }

    internal void AddField(PackField field)
    {
        if (_fields.ContainsKey(field.Identifier))
        {
            throw new PackContentException($"Pack {field.FullyQualifiedIdentifier} defined multiple times.");
        }
        _fields.Add(field.Identifier, field);
    }
    internal void AddProperty(PackProperty property)
    {
        if (_properties.ContainsKey(property.Identifier))
        {
            throw new PackContentException($"Pack {property.FullyQualifiedIdentifier} defined multiple times.");
        }
        _properties.Add(property.Identifier, property);
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