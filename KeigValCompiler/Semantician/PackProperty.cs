using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackProperty
{
    // Internal fields.
    internal string Name { get; private init; }
    internal string FullName { get; private init; }
    internal string Type { get; private init; }
    internal string DefaultValue { get; private init; }
    internal PackFunction? GetFunction { get; private init; }
    internal PackFunction? SetFunction { get; private init; }
    internal PackFunction? InitFunction { get; private init; }


    // Constructors.
    internal PackProperty(string name, PackFunction getFunc, PackFunction setFunc, PackFunction initFunc, string type, string defaultValue, PackClass packClass)
        : this(name, getFunc, setFunc, initFunc, type, defaultValue)
    {
        FullName = $"{packClass.FullName}.{Name}";
    }

    internal PackProperty(string name, PackFunction getFunc, PackFunction setFunc, PackFunction initFunc, string type, string defaultValue, string nameSpace)
        : this(name, getFunc, setFunc, initFunc, type, defaultValue)
    {
        FullName = $"{nameSpace}.{Name}";
    }

    private PackProperty(string name, PackFunction getFunc, PackFunction setFunc, PackFunction initFunc, string type, string defaultValue)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        GetFunction = getFunc ?? throw new ArgumentNullException(nameof(getFunc));
        SetFunction = setFunc ?? throw new ArgumentNullException(nameof(setFunc));
        DefaultValue = defaultValue ?? throw new ArgumentNullException(nameof(defaultValue));
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }
}