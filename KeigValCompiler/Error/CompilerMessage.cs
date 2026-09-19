using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

/* A single queued error or warning. Note that the location is captured when the message is created,
 * not when it is printed, because error recovery moves the source parser's cursor long before
 * anything gets printed. */
internal class CompilerMessage
{
    // Fields.
    /* Null only for the placeholder messages which still carry their text as a literal instead of
     * coming from a definition in the ErrorRepository. See StatementParser. */
    internal CompilerMessageDefinition? Definition { get; private init; }
    internal string Message { get; private init; }
    internal string? Notes { get; private init; }
    internal CompilerMessageLocation Location { get; private init; }
    internal bool IsError { get; private init; }


    // Constructors.
    internal CompilerMessage(ErrorCreateOptions error, CompilerMessageLocation location, string? notes)
    {
        Definition = error.Definition;
        Message = error.CreateMessage();
        Location = location;
        Notes = notes;
        IsError = true;
    }

    internal CompilerMessage(WarningCreateOptions warning, CompilerMessageLocation location, string? notes)
    {
        Definition = warning.Definition;
        Message = warning.CreateMessage();
        Location = location;
        Notes = notes;
        IsError = false;
    }

    internal CompilerMessage(string message, CompilerMessageLocation location, string? notes)
    {
        Definition = null;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Location = location;
        Notes = notes;
        IsError = true;
    }


    // Inherited methods.
    public override string ToString()
    {
        StringBuilder MessageBuilder = new();

        string LocationText = Location.ToString();
        if (LocationText.Length > 0)
        {
            MessageBuilder.Append($"{LocationText}: ");
        }
        if (Message.Length > 0)
        {
            MessageBuilder.Append($"{Message}{GetEndPunctuation(Message)} ");
        }
        if (Notes != null)
        {
            MessageBuilder.Append($"Notes: {Notes}{GetEndPunctuation(Notes)}");
        }

        return MessageBuilder.ToString().TrimEnd();
    }


    // Private static methods.
    private static string GetEndPunctuation(string sentence)
    {
        if ((sentence.Length == 0) || (sentence.EndsWith('.') || sentence.EndsWith('?') || sentence.EndsWith('!')))
        {
            return string.Empty;
        }
        return ".";
    }
}