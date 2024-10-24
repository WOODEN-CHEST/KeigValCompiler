namespace KeigValCompiler.Semantician.Member;

internal class LocalField : IIdentifiable
{
    // Fields.
    public Identifier SelfIdentifier { get; private init; }


    // Internal fields.
    internal Identifier Type { get; set; }


    // Constructors.
    internal LocalField(Identifier name)
    {
        SelfIdentifier = name ?? throw new ArgumentNullException(nameof(name));
    }
}