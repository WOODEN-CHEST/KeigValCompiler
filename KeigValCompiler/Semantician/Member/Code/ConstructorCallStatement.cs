using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class ConstructorCallStatement : FunctionCallStatement
{
    // Fields.
    internal TypeTargetIdentifier ObjectType { get; set; }

    /* The braced section of "new Thing { A = 1 }" or "new List { 1, 2 }", or null when the
     * construction has no initializer. Member initializers are AssignmentStatements, while
     * collection initializers are plain value statements. */
    internal StatementCollection? Initializer { get; set; } = null;


    // Constructors.
    internal ConstructorCallStatement(TypeTargetIdentifier objectType, params Statement[] args) : base(args)
    {
        ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children =>
        base.Children.Concat(Initializer ?? Enumerable.Empty<Statement>());


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        base.TransformChildren(transform);
        Initializer?.TransformAll(transform);
    }
}
