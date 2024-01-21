using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackFunction
{
    // Internal fields.
    internal PackMemberModifiers Modifiers { get; set; }
    internal string Name { get; private init; }
    internal PackClass? ParentClass { get; private init; }
    internal string NameSpace { get; private init; }


    // Private fields.
    private Dictionary<string, FunctionParamater> _parameters = new();


    // Constructors.
    internal PackFunction(PackClass parentClass, string name, PackMemberModifiers modifiers, FunctionParamater[] paramaters)
    {
        ParentClass = parentClass ?? throw new ArgumentNullException(nameof(parentClass));
        NameSpace = parentClass.NameSpace;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        LoadParamaters(paramaters);
    }

    internal PackFunction(string nameSpace, string name, PackMemberModifiers modifiers, FunctionParamater[] paramaters)
    {
        ParentClass = null;
        NameSpace = nameSpace ?? throw new ArgumentNullException(nameof(nameSpace));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        LoadParamaters(paramaters);
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
            _parameters.Add(Paramater.Name, Paramater)
        }
    }
}