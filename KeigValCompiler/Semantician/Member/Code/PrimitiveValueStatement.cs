using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class PrimitiveValueStatement : Statement
{
    // Internal fields.
    internal string Value
    {
        get => _value;
        set => _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal Identifier PrimitiveValueType { get; set; }


    // Private fields.
    private string _value;

    
    // Constructors.
    internal PrimitiveValueStatement(string value)
    {
        Value = value;
    }
}