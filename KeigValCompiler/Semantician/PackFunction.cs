using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackFunction
{
    // Internal fields.
    
    internal string Name { get; private init; }
    internal string NameSpace { get; private init; }
    internal string FullName { get; private init; }
    internal PackClass? ParentClass { get; private init; }
    internal PackMemberModifiers Modifiers { get; set; }


    // Private fields.
    private Dictionary<string, FunctionParamater> _parameters = new();


    // Constructors.
    internal PackFunction(PackClass parentClass, string name, PackMemberModifiers modifiers, FunctionParamater[] paramaters)
    {
        ParentClass = parentClass ?? throw new ArgumentNullException(nameof(parentClass));
        NameSpace = parentClass.NameSpace;
        Name = name ?? throw new ArgumentNullException(nameof(name));

        LoadParamaters(paramaters);
        FullName = $"{ParentClass.FullName}.{Name}={GetHashCode()}";
    }

    internal PackFunction(string nameSpace, string name, PackMemberModifiers modifiers, FunctionParamater[] paramaters)
    {
        ParentClass = null;
        NameSpace = nameSpace ?? throw new ArgumentNullException(nameof(nameSpace));
        Name = name ?? throw new ArgumentNullException(nameof(name));

        LoadParamaters(paramaters);
        FullName = $"{NameSpace}.{Name}={GetHashCode()}";
    }


    // Internal methods.
    internal FunctionParamater? GetParamater(string name)
    {
        _parameters.TryGetValue(name, out var Paramater);
        return Paramater;
    }

    internal void RemoveParamater(string name) => _parameters.Remove(name);


    // Private methods.
    private void LoadParamaters(FunctionParamater[] paramaters)
    {
        foreach (FunctionParamater Paramater in paramaters)
        {
            _parameters.Add(Paramater.Name, Paramater);
        }
    }


    // Inherited methods.
    public override int GetHashCode()
    {
        int HashCode = FullName.GetHashCode();
        foreach (FunctionParamater paramater in _parameters.Values)
        {
            HashCode += paramater.GetHashCode();
        }

        return HashCode;
    }

    public override bool Equals(object? obj)
    {
        if (obj is PackClass)
        {
            return FullName == ((PackFunction)obj).FullName;
        }
        return false;
    }

    public override string ToString()
    {
        return FullName;
    }
}