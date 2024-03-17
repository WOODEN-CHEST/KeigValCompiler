using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackFunction : PackMember
{
    // Internal fields.


    // Private fields.
    private Dictionary<string, FunctionParamater> _parameters = new();


    // Constructors.
    internal PackFunction(string identifier, PackMemberModifiers modifiers, PackClass parentClass, FunctionParamater[] paramaters)
        : base(identifier, modifiers, parentClass)
    {
        foreach (FunctionParamater Paramater in paramaters)
        {
            _parameters.Add(Paramater.Name, Paramater);
        }
    }

    internal PackFunction(string identifier,
        PackMemberModifiers modifiers,
        string parentNamespace,
        PackSourceFile sourceFile,
        FunctionParamater[] paramaters)
        : base(identifier, modifiers, parentNamespace, sourceFile)
    {
        foreach (FunctionParamater Paramater in paramaters)
        {
            _parameters.Add(Paramater.Name, Paramater);
        }
    }


    // Internal methods.
    internal FunctionParamater? GetParamater(string name)
    {
        _parameters.TryGetValue(name, out var Paramater);
        return Paramater;
    }


    // Private methods.


    // Inherited methods.
    public override int GetHashCode()
    {
        int HashCode = FullyQualifiedIdentifier.GetHashCode();
        foreach (FunctionParamater paramater in _parameters.Values)
        {
            HashCode += paramater.GetHashCode();
        }

        return HashCode;
    }
}