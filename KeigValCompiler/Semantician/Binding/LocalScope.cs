namespace KeigValCompiler.Semantician.Binding;

/* One level of the names a body declares itself, which a name in the body is looked for in before anything
 * else, from the innermost level out: the locals of a block, or the parameters of the function the body
 * belongs to. */
internal abstract class LocalScope
{
    // Internal fields.
    internal LocalScope? Parent { get; private init; }


    // Constructors.
    internal LocalScope(LocalScope? parent)
    {
        Parent = parent;
    }
}
