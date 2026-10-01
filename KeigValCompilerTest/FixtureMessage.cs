using KeigValCompiler.Error;

namespace KeigValCompilerTest;

/* A message as a fixture's header lists it: the line, the prefix of its category, whether it is an error,
 * and its code. The text is left out, so that rewording a message breaks no fixture. */
internal sealed record FixtureMessage(int Line, string Prefix, bool IsError, int Code)
{
    // Static fields.
    /* For the few messages which come from no definition, and so have no category or code. */
    private const string UNKNOWN_PREFIX = "?";


    // Internal static methods.
    internal static FixtureMessage Of(CompilerMessage message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        return new(message.Location.Line, message.Definition?.Category.Prefix ?? UNKNOWN_PREFIX,
            message.IsError, message.Definition?.Code ?? 0);
    }


    // Inherited methods.
    public override string ToString()
    {
        return $"line {Line} {Prefix} {(IsError ? "error" : "warning")} {Code}";
    }
}
