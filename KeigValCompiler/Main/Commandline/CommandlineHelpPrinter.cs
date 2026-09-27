using System.Text;

namespace KeigValCompiler.Main.Commandline;

/* Builds the help text from nothing but what is registered in a repository, so an argument is documented
 * just by being registered. Positional arguments are listed in the order they are expected in, and named
 * ones alphabetically. */
internal class CommandlineHelpPrinter
{
    // Private fields.
    private const string INDENT = "  ";
    private const string SHORT_NAME_SEPARATOR = ", ";
    private const string OPTIONS_PLACEHOLDER = "[options]";

    private const char WORD_SEPARATOR = ' ';

    /* The least space kept between an argument and its description. */
    private const int DESCRIPTION_GAP = 2;

    /* Descriptions are wrapped to keep lines within this, which is the narrowest terminal still common. */
    private const int LINE_WIDTH = 80;

    /* Should an argument be written so long that it pushes its description almost to the end of the line,
     * the description gets at least this much room per line and overruns the line width instead. */
    private const int MIN_DESCRIPTION_WIDTH = 30;


    // Methods.
    internal void PrintHelp(string programName, CommandlineArgumentRepository repository)
    {
        ArgumentNullException.ThrowIfNull(programName, nameof(programName));
        ArgumentNullException.ThrowIfNull(repository, nameof(repository));

        CommandlineNamedArgument[] SortedNamedArguments = repository.NamedArguments
            .OrderBy(Argument => Argument.LongName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(Argument => Argument.LongName, StringComparer.Ordinal)
            .ToArray();

        string[] PositionalUsages = repository.PositionalArguments
            .Select(Argument => Argument.DisplayName)
            .ToArray();
        string[] PositionalDescriptions = repository.PositionalArguments
            .Select(Argument => Argument.Description)
            .ToArray();
        string[] NamedUsages = SortedNamedArguments.Select(GetNamedArgumentUsage).ToArray();
        string[] NamedDescriptions = SortedNamedArguments.Select(GetNamedArgumentDescription).ToArray();

        /* Both sections share one description column, so that they line up with each other as well. */
        int DescriptionColumn = PositionalUsages.Concat(NamedUsages)
            .Select(Usage => Usage.Length)
            .DefaultIfEmpty(0)
            .Max() + DESCRIPTION_GAP;

        StringBuilder HelpBuilder = new();
        HelpBuilder.AppendLine($"Usage: {GetUsageLine(programName, repository)}");
        AppendSection(HelpBuilder, "Arguments:", PositionalUsages, PositionalDescriptions, DescriptionColumn);
        AppendSection(HelpBuilder, "Options:", NamedUsages, NamedDescriptions, DescriptionColumn);

        Console.Write(HelpBuilder.ToString());
    }


    // Private methods.
    private string GetUsageLine(string programName, CommandlineArgumentRepository repository)
    {
        StringBuilder UsageBuilder = new(programName);

        foreach (CommandlinePositionalArgument Argument in repository.PositionalArguments)
        {
            UsageBuilder.Append($" {Argument.DisplayName}");
        }
        if (repository.NamedArguments.Count > 0)
        {
            UsageBuilder.Append($" {OPTIONS_PLACEHOLDER}");
        }

        return UsageBuilder.ToString();
    }

    /* An argument without a short name is padded by as much as a short name takes up, so that every long
     * name starts in the same column. */
    private string GetNamedArgumentUsage(CommandlineNamedArgument argument)
    {
        string ShortNameUsage = argument.ShortName.HasValue
            ? $"{CommandlineSyntax.SHORT_NAME_PREFIX}{argument.ShortName.Value}{SHORT_NAME_SEPARATOR}"
            : new string(' ', CommandlineSyntax.SHORT_NAME_TOKEN_LENGTH + SHORT_NAME_SEPARATOR.Length);

        string ValueUsage = string.Empty;
        if (argument is CommandlineOption Option)
        {
            ValueUsage = $" {CommandlineSyntax.VALUE_NAME_START}{Option.ValueName}{CommandlineSyntax.VALUE_NAME_END}";
        }

        return $"{ShortNameUsage}{argument.DisplayName}{ValueUsage}";
    }

    private string GetNamedArgumentDescription(CommandlineNamedArgument argument)
    {
        if ((argument is CommandlineOption Option) && Option.IsRepeatable)
        {
            return $"{argument.Description} Can be given more than once.";
        }
        return argument.Description;
    }

    private void AppendSection(StringBuilder helpBuilder,
        string title,
        string[] usages,
        string[] descriptions,
        int descriptionColumn)
    {
        if (usages.Length == 0)
        {
            return;
        }

        helpBuilder.AppendLine();
        helpBuilder.AppendLine(title);
        for (int Index = 0; Index < usages.Length; Index++)
        {
            AppendEntry(helpBuilder, usages[Index], descriptions[Index], descriptionColumn);
        }
    }

    /* The description is wrapped at word boundaries, each continuation line starting in the description
     * column. A single word longer than a whole line is left whole rather than broken up. */
    private void AppendEntry(StringBuilder helpBuilder, string usage, string description, int descriptionColumn)
    {
        string ContinuationIndent = new(WORD_SEPARATOR, INDENT.Length + descriptionColumn);
        int DescriptionWidth = Math.Max(MIN_DESCRIPTION_WIDTH, LINE_WIDTH - ContinuationIndent.Length);

        helpBuilder.Append(INDENT);
        helpBuilder.Append(usage.PadRight(descriptionColumn));

        int LineLength = 0;
        foreach (string Word in description.Split(WORD_SEPARATOR, StringSplitOptions.RemoveEmptyEntries))
        {
            if ((LineLength > 0) && ((LineLength + 1 + Word.Length) > DescriptionWidth))
            {
                helpBuilder.AppendLine();
                helpBuilder.Append(ContinuationIndent);
                LineLength = 0;
            }
            if (LineLength > 0)
            {
                helpBuilder.Append(WORD_SEPARATOR);
                LineLength++;
            }

            helpBuilder.Append(Word);
            LineLength += Word.Length;
        }

        helpBuilder.AppendLine();
    }
}
