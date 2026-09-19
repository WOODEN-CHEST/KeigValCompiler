namespace KeigValCompiler.Semantician.Member.Code;

/* One piece of an interpolated string: either a run of literal text, or a statement whose value is
 * substituted into the string. Exactly one of Text and Value is set. */
internal class InterpolatedStringSection
{
    // Fields.
    internal string? Text { get; private init; }
    internal Statement? Value { get; set; }
    internal bool IsLiteral => Text != null;


    // Constructors.
    internal InterpolatedStringSection(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    internal InterpolatedStringSection(Statement value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}
