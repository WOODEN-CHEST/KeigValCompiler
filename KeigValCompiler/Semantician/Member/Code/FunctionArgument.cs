namespace KeigValCompiler.Semantician.Member.Code;

/* A single argument at a call site, which may carry a parameter name ("Draw(width: 5)") and a
 * modifier ("TryGet(out Result)") in addition to its value. */
internal class FunctionArgument
{
    // Fields.
    internal Statement Value { get; set; }

    /* Null for a positional argument; set when the argument names its parameter. */
    internal Identifier? Name { get; set; } = null;
    internal FunctionParameterModifier Modifier { get; set; } = FunctionParameterModifier.None;

    internal bool IsNamed => Name != null;


    // Constructors.
    internal FunctionArgument(Statement value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal FunctionArgument(Statement value, Identifier? name, FunctionParameterModifier modifier)
        : this(value)
    {
        Name = name;
        Modifier = modifier;
    }
}
