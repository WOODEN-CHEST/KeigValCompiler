namespace KeigValCompiler.Main.Commandline;

/* Something the compiler can be told on the command line. Each kind of argument derives from this and
 * tells CommandlineParser how it is written and whether it carries a value. */
internal abstract class CommandlineArgument
{
    // Fields.
    internal string Description { get; private init; }

    /* How the argument is referred to in the help text and in error messages. */
    internal abstract string DisplayName { get; }


    // Constructors.
    internal CommandlineArgument(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}
