namespace KeigValCompiler.Error;

/* Where the messages about one thing go: the collection gathering them, which is the stage's own, or one held
 * back while it is not known whether they will be kept, and the place in the source they point at. */
internal readonly struct MessageTarget
{
    // Internal fields.
    internal CompilerMessageCollection Messages { get; private init; }
    internal CompilerMessageLocation Location { get; private init; }


    // Constructors.
    internal MessageTarget(CompilerMessageCollection messages, CompilerMessageLocation location)
    {
        Messages = messages ?? throw new ArgumentNullException(nameof(messages));
        Location = location;
    }


    // Internal methods.
    internal void AddError(ErrorCreateOptions error)
    {
        Messages.AddError(error, Location, null);
    }
}
