using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class VariableAssignment : IIdentifiable
{
    // Fields.
    public Identifier SelfIdentifier { get; private init; }


    // Internal fields.
    internal Statement? Value { get; set; }


    // Constructors.
    internal VariableAssignment(Identifier identifier, Statement? value = null)
    {
        SelfIdentifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        Value = value;
    }
}