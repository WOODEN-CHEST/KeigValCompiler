namespace KeigValCompiler.Semantician.Member;

internal class LocalField : IIdentifiable
{
    // Fields.
    public Identifier SelfIdentifier { get; private init; }


    // Internal fields.
    internal Identifier Type { get; private init; }


    // Constructors.
    internal LocalField(Identifier type, Identifier name)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        SelfIdentifier = name ?? throw new ArgumentNullException(nameof(name));
    }
}