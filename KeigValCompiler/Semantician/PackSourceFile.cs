using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackSourceFile
{
    // Internal fields.
    internal DataPack Pack { get; private set; }
    internal string[] NamespaceImports => _namespaceImports.ToArray();
    internal PackNameSpace[] Namespaces => _namespaces.ToArray();


    // Private fields.
    private readonly HashSet<string> _namespaceImports = new();
    private readonly HashSet<PackNameSpace> _namespaces = new();


    // Constructors.
    internal PackSourceFile(DataPack pack)
    {
        Pack = pack ?? throw new ArgumentNullException(nameof(pack));
    }


    // Internal methods.
    internal void AddNamespaceImport(string name)
    {
        _namespaceImports.Add(name);
    }

    internal void AddNamespace(PackNameSpace nameSpace)
    {
        _namespaces.Add(nameSpace);
    }
}