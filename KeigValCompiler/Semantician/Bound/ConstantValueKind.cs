namespace KeigValCompiler.Semantician.Bound;

/* What a constant value holds. Which integer type an integer constant has is its expression's type, which is
 * also what makes a constant an enum's. */
internal enum ConstantValueKind
{
    Integer,
    Char,
    Boolean,
    String,
    Decimal,
    Null
}
