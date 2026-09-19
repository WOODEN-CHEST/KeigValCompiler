using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class ForEachStatement : Statement
{
    // Fields.
    /* Null means the element type was inferred with the "var" keyword. */
    internal TypeTargetIdentifier? ElementType { get; set; } = null;
    internal Identifier ElementName { get; set; }
    internal Statement EnumeratorProvider { get; set; }
    internal StatementCollection Body { get; } = new();


    // Constructors.
    public ForEachStatement(Identifier elementName, Statement enumeratorGetter)
    {
        ElementName = elementName ?? throw new ArgumentNullException(nameof(elementName));
        EnumeratorProvider = enumeratorGetter ?? throw new ArgumentNullException(nameof(enumeratorGetter));
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children =>
        new Statement[] { EnumeratorProvider }.Concat(Body);


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        EnumeratorProvider = transform(EnumeratorProvider);
        Body.TransformAll(transform);
    }
}
