using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackField
{
    // Internal fields.
    internal string Name { get; private init; }
    internal string FullName { get; private init; }
    internal string Type { get; private init; }
    internal string InitialValue { get; private init; }


    // Constructors.
    internal PackField(string name, string type, PackClass packClass, string initialValue) : this(name, type, initialValue)
    {
        FullName = $"{packClass.FullName}.{name}";
    }

    internal PackField(string name, string type, string initialValue, string nameSpace) : this(name, type, initialValue)
    {
        FullName = $"{nameSpace}.{name}";
    }

    private PackField(string name, string type, string initialValue)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        InitialValue = initialValue ?? throw new ArgumentNullException(nameof(initialValue));
    }
}