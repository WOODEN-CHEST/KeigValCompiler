using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Statement;

internal interface IValueStatement
{
    public string ValueType { get; }

    public string? TryGetConstantValue();
}