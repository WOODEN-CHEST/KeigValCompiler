using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Generics;

internal class GenericTypeParameter : IIdentifiable
{
    // Fields.
    public Identifier SelfIdentifier { get; }
    public GenericConstraint[] Constraints => _constraints.ToArray();


    // Private fields.
    private readonly GenericConstraint[] _constraints;


    // Constructors.
    internal GenericTypeParameter(Identifier identifier, GenericConstraint[] constraints)
    {
        SelfIdentifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        _constraints = constraints?.ToArray() ?? throw new ArgumentNullException(nameof(constraints));
    }
}