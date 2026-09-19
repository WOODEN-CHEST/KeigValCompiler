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


    // Private fields.
    private object _value;

    
    // Constructors.
    internal PrimitiveValueStatement(object value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => Array.Empty<Statement>();
}
