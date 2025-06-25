using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class PrimitiveValueStatement : Statement
{
    // Internal fields.
    internal object Value
    {
        get => _value;
        set => _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal Identifier PrimitiveValueType { get; set; }


    // Private fields.
    private object _value;

    
    // Constructors.
    internal PrimitiveValueStatement(object value)
    {
        Value = value;
    }
}