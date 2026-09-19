namespace KeigValCompiler.Semantician.Member.Code;

/* An interpolated string such as $"a{b}c", held as an ordered run of literal and substituted
 * sections. */
internal class InterpolatedStringStatement : Statement
{
    // Fields.
    internal IEnumerable<InterpolatedStringSection> Sections => _sections;
    internal int SectionCount => _sections.Count;


    // Private fields.
    private readonly List<InterpolatedStringSection> _sections = new();


    // Methods.
    internal void AddSection(InterpolatedStringSection section)
    {
        _sections.Add(section ?? throw new ArgumentNullException(nameof(section)));
    }

    internal void ClearSections()
    {
        _sections.Clear();
    }


    // Inherited fields.
    internal override IEnumerable<Statement> Children =>
        _sections.Where(section => section.Value != null).Select(section => section.Value!);


    // Inherited methods.
    internal override void TransformChildren(Func<Statement, Statement> transform)
    {
        foreach (InterpolatedStringSection Section in _sections)
        {
            if (Section.Value != null)
            {
                Section.Value = transform(Section.Value);
            }
        }
    }
}
