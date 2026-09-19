namespace KeigValCompiler.Semantician.Member.Code;

/* Creation of an array, either sized as in "new int[5]" or populated as in "new int[] { 1, 2, 3 }".
 * At least one of Lengths and Elements is expected to be set. */
internal class ArrayCreationStatement : Statement
{
    // Fields.
    /* Null means the element type was left out, as in "new[] { 1, 2, 3 }". */
    internal TypeTargetIdentifier? ElementType { get; set; }

    /* One length statement per dimension, as written in "new int[x][y]". Empty when the array is
     * created from an element list instead. */
    internal IEnumerable<Statement> Lengths => _lengths;

    /* The values the array is initialised with, or null when no element list was given. */
    internal StatementCollection? Elements { get; set; } = null;


    // Private fields.
    private readonly List<Statement> _lengths = new();


    // Constructors.
    internal ArrayCreationStatement(TypeTargetIdentifier? elementType)
    {
        ElementType = elementType;
    }


    // Methods.
    internal void AddLength(Statement length)
    {
        _lengths.Add(length ?? throw new ArgumentNullException(nameof(length)));
    }

    internal void ClearLengths()
    {
        _lengths.Clear();
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children =>
        _lengths.Concat(Elements ?? Enumerable.Empty<Statement>());


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        for (int i = 0; i < _lengths.Count; i++)
        {
            _lengths[i] = transform(_lengths[i]);
        }
        Elements?.TransformAll(transform);
    }
}
