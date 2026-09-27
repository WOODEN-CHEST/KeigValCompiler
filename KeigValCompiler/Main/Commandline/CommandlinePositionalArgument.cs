namespace KeigValCompiler.Main.Commandline;

/* An argument found by its position among the ones which are not named, like the source directory
 * being the first of them. Only CommandlinePositionalArgument<T> knows what type its value is; this is
 * the part of it the parser works with.
 * Every positional argument is required for now. Allowing optional ones means first deciding how they
 * interact with the required ones after them. */
internal abstract class CommandlinePositionalArgument : CommandlineArgument
{
    // Fields.
    /* What the value is called, like "source directory", which is shown as "<source directory>". */
    internal string Name { get; private init; }
    internal override string DisplayName
        => $"{CommandlineSyntax.VALUE_NAME_START}{Name}{CommandlineSyntax.VALUE_NAME_END}";


    // Constructors.
    internal CommandlinePositionalArgument(string name, string description) : base(description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }


    // Methods.
    /* When the text is not a valid value, the error saying why is queued into the context and false is
     * returned. */
    internal abstract bool TryParseValue(string text, CommandlineParsingContext context, out object? value);
}
