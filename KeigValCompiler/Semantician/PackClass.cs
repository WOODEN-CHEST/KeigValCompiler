using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackClass
{
    // Internal fields.
    internal string NameSpace { get; private init; }
    internal string Name { get; private init; }
    internal string FullName => $"{NameSpace}.{Name}";


    // Constructors.
    internal PackClass(string nameSpace, string name)
    {
        NameSpace = nameSpace ?? throw new ArgumentNullException(nameof(nameSpace));
        Name = name ?? throw new ArgumentNullException(nameof(nameSpace));
    }
}