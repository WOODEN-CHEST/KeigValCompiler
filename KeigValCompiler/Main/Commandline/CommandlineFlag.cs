namespace KeigValCompiler.Main.Commandline;

/* A named argument which carries no value. Being present is all it says, like "--help". */
internal class CommandlineFlag : CommandlineNamedArgument
{
    // Constructors.
    internal CommandlineFlag(string longName, char? shortName, string description)
        : base(longName, shortName, description) { }
}
