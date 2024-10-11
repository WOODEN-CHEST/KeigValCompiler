using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Generics;

internal class GenericConstraint
{
    // Fields.
    internal IIdentifiable Constraint { get; private init; }


    // Constructors.
    internal GenericConstraint(IIdentifiable constraintIdentifier)
    {
        Constraint = constraintIdentifier ?? throw new ArgumentNullException(nameof(constraintIdentifier));
    }
}