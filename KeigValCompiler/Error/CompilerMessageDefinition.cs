using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

internal abstract class CompilerMessageDefinition
{
    // Fields.
    public int Code { get; init; }
    public string RawMessage { get; init; }
    public CompilerMessageCategory Category { get; init; }


    // Constructors.
    public CompilerMessageDefinition(int code, string rawMessage, CompilerMessageCategory category)
    {
        Code = code;
        RawMessage = rawMessage ?? throw new ArgumentNullException(nameof(rawMessage));
        Category = category ?? throw new ArgumentNullException(nameof(category));
    }


    // Methods.
    /* Raw messages are hand-written format strings holding hand-counted placeholders, so sooner or
     * later one of them will not match the arguments it is given. A compiler which crashes while
     * describing someone else's mistake is worse than one which describes it badly. */
    internal string FormatRawMessage(object[]? arguments)
    {
        try
        {
            return string.Format(CultureInfo.InvariantCulture, RawMessage, arguments ?? Array.Empty<object>());
        }
        catch (FormatException)
        {
            return GetUnformattableMessage();
        }
        catch (ArgumentException)
        {
            return GetUnformattableMessage();
        }
    }


    // Private methods.
    private string GetUnformattableMessage()
    {
        return $"(This message could not be filled in - the message text of {Category.Name} {Code} "
            + $"does not match the values the compiler passed to it. This is a compiler bug.) {RawMessage}";
    }
}