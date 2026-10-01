using KeigValCompiler.Semantician.Bound;

namespace KeigValCompiler.Semantician.Binding;

/* The locals a block declares directly. As in C#, each belongs to the whole block, so that a name in the block
 * finds it even before its declaration, which is then an error; each is declared once binding reaches its
 * declaration. */
internal sealed class BlockScope : LocalScope
{
    // Internal fields.
    /* In the order they are declared in. */
    internal IReadOnlyList<LocalSymbol> Locals => _locals;


    // Private fields.
    private readonly List<LocalSymbol> _locals = new();
    private readonly Dictionary<string, LocalSymbol> _localsByName = new();
    private readonly HashSet<LocalSymbol> _declared = new(ReferenceEqualityComparer.Instance);


    // Constructors.
    internal BlockScope(LocalScope? parent) : base(parent) { }


    // Internal methods.
    /* Adds a local the block declares, unless the block already declares one of its name, which is then the
     * one the name finds. */
    internal bool TryAdd(LocalSymbol local)
    {
        ArgumentNullException.ThrowIfNull(local, nameof(local));

        if (!_localsByName.TryAdd(local.Name, local))
        {
            return false;
        }
        _locals.Add(local);
        return true;
    }

    internal LocalSymbol? Find(string name)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        _localsByName.TryGetValue(name, out LocalSymbol? Local);
        return Local;
    }

    internal void MarkDeclared(LocalSymbol local)
    {
        ArgumentNullException.ThrowIfNull(local, nameof(local));
        _declared.Add(local);
    }

    internal bool IsDeclared(LocalSymbol local)
    {
        ArgumentNullException.ThrowIfNull(local, nameof(local));
        return _declared.Contains(local);
    }
}
