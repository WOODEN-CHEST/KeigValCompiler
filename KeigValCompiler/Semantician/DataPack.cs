using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace KeigValCompiler.Semantician;

internal class DataPack
{
    // Private fields.
    private readonly Dictionary<string, PackNameSpace> _namespaces = new();
    


    // Constructors.
    internal DataPack()
    {
        
    }


    // Internal methods.
    internal PackNameSpace GetNamespace(string nameSpace)
    {
        if (!_namespaces.ContainsKey(nameSpace))
        {
            PackNameSpace NameSpace = new(nameSpace);
            _namespaces.Add(NameSpace.Name, NameSpace);
            return NameSpace;
        }

        return _namespaces[nameSpace];
    }

    internal void BuildInfo()
    {

    }
}