namespace KeigValCompiler.Semantician.Member.Code;

/* A declaration of one or more new variables, as in "int a = 1, b = 2;". Assigning to a variable
 * that already exists is an AssignmentStatement instead. */
internal class VariableDeclarationStatement : Statement
{
    // Internal fields.
    /* Null means the type was inferred with the "var" keyword. */
    internal TypeTargetIdentifier? Type { get; set; }
    internal bool IsTypeInferred => Type == null;
    internal VariableAssignmentCollection Declarations { get; } = new();


    // Constructors.
    internal VariableDeclarationStatement(TypeTargetIdentifier? type)
    {
        Type = type;
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => Declarations
        .Where(declaration => declaration.Value != null).Select(declaration => declaration.Value!);


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        foreach (VariableAssignment Declaration in Declarations)
        {
            if (Declaration.Value != null)
            {
                Declaration.Value = transform(Declaration.Value);
            }
        }
    }
}
