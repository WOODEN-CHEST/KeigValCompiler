using System.Diagnostics.CodeAnalysis;

namespace KeigValCompiler.Main.Commandline;

/* Reads the path of a directory. A relative path is taken from the working directory the compiler was
 * started in, and is handed on fully qualified so that nothing later depends on where that was. */
internal class DirectoryPathValueParser : ICommandlineValueParser<string>
{
    // Fields.
    /* A directory which is read from has to exist, while one which is written to may be created later. */
    internal bool IsExistenceRequired { get; private init; }


    // Constructors.
    internal DirectoryPathValueParser(bool isExistenceRequired)
    {
        IsExistenceRequired = isExistenceRequired;
    }


    // Inherited methods.
    public bool TryParseValue(string text,
        CommandlineArgument argument,
        CommandlineParsingContext context,
        [MaybeNullWhen(false)] out string value)
    {
        ArgumentNullException.ThrowIfNull(text, nameof(text));
        ArgumentNullException.ThrowIfNull(argument, nameof(argument));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        value = null;
        string FullPath;
        try
        {
            FullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(text));
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            context.AddError(context.ErrorCreator.CommandlineInvalidPath.CreateOptions(text, argument.DisplayName));
            return false;
        }

        if (File.Exists(FullPath))
        {
            context.AddError(context.ErrorCreator.CommandlinePathIsFile
                .CreateOptions(FullPath, argument.DisplayName));
            return false;
        }
        if (IsExistenceRequired && !Directory.Exists(FullPath))
        {
            context.AddError(context.ErrorCreator.CommandlineDirectoryNotFound
                .CreateOptions(FullPath, argument.DisplayName));
            return false;
        }

        value = FullPath;
        return true;
    }
}
