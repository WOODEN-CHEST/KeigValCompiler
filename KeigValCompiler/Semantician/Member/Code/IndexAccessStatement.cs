namespace KeigValCompiler.Semantician.Member.Code;

/* An indexed access such as "array[i]" or "dictionary[key]". The target is a statement rather than
 * an identifier so that chained accesses like "GetItems()[0]" can be represented. */
internal class IndexAccessStatement : Statement
{
    // Fields.
    internal Statement Target { get; set; }
    internal IEnumerable<Statement> Indices => _indices;
    internal int IndexCount => _indices.Count;


    // Private fields.
    private readonly List<Statement> _indices = new();


    // Constructors.
    internal IndexAccessStatement(Statement target, params Statement[]? indices)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        if (indices != null)
        {
            _indices.AddRange(indices);
        }
    }


    // Methods.
    internal void AddIndex(Statement index)
    {
        _indices.Add(index ?? throw new ArgumentNullException(nameof(index)));
    }

    internal void ClearIndices()
    {
        _indices.Clear();
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children => new Statement[] { Target }.Concat(_indices);


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        Target = transform(Target);
        for (int i = 0; i < _indices.Count; i++)
        {
            _indices[i] = transform(_indices[i]);
        }
    }
}
