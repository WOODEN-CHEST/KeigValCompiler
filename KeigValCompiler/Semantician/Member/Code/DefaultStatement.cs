namespace KeigValCompiler.Semantician.Member.Code;

/* The "default" operator, which resolves to a type's default value. */
internal class DefaultStatement : Statement
{
    // Fields.
    /* Null means the bare "default" form was used, whose type is inferred from context. */
    internal TypeTargetIdentifier? TargetType { get; set; }


    // Constructors.
    internal DefaultStatement(TypeTargetIdentifier? targetType)
    {
        TargetType = targetType;
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => Array.Empty<Statement>();
}
