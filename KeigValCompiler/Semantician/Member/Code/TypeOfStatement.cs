namespace KeigValCompiler.Semantician.Member.Code;

/* The "typeof" operator, which resolves to a type's runtime representation. */
internal class TypeOfStatement : Statement
{
    // Fields.
    internal TypeTargetIdentifier TargetType { get; set; }


    // Constructors.
    internal TypeOfStatement(TypeTargetIdentifier targetType)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => Array.Empty<Statement>();
}
