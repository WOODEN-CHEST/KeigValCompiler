using System.Text.RegularExpressions;

namespace KeigValCompilerTest;

/* What the comment a fixture file starts with says the compiler reports for that file: one entry per
 * message, written as "line 12 RS error 2   what it is about", and the counts it states, where it states
 * any. A file whose comment lists no entries, or which has none, expects no messages at all. */
internal sealed class FixtureHeader
{
    // Static fields.
    private const string COMMENT_START = "/*";
    private const string COMMENT_END = "*/";
    private const string KIND_ERROR = "error";

    private static readonly Regex _entryPattern =
        new(@"^\s*\*\s+line (\d+) ([A-Z]+)\s+(error|warning) (\d+)\b");
    private static readonly Regex _fileCountPattern =
        new(@"Expected (?:output|from this file), (\d+) errors?(?: and (\d+) warnings?)?");
    private static readonly Regex _directoryCountPattern =
        new(@"The whole directory gives (\d+) errors? and (\d+) warnings?");


    // Internal fields.
    internal IReadOnlyList<FixtureMessage> Entries { get; private init; } = Array.Empty<FixtureMessage>();

    /* The errors and warnings the comment says the file gives, or null when it does not say. */
    internal (int Errors, int Warnings)? StatedFileCounts { get; private init; }

    /* The errors and warnings the comment says the whole directory gives, or null when it does not say. */
    internal (int Errors, int Warnings)? StatedDirectoryCounts { get; private init; }


    // Constructors.
    private FixtureHeader() { }


    // Internal static methods.
    internal static FixtureHeader Read(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath, nameof(filePath));

        string Text = File.ReadAllText(filePath).TrimStart('﻿');
        int CommentEnd = Text.IndexOf(COMMENT_END, StringComparison.Ordinal);
        if (!Text.StartsWith(COMMENT_START, StringComparison.Ordinal) || (CommentEnd == -1))
        {
            return new();
        }

        string[] Lines = Text[..CommentEnd].Split('\n');
        List<FixtureMessage> Entries = new();
        foreach (string Line in Lines)
        {
            Match EntryMatch = _entryPattern.Match(Line);
            if (EntryMatch.Success)
            {
                Entries.Add(new(int.Parse(EntryMatch.Groups[1].Value), EntryMatch.Groups[2].Value,
                    EntryMatch.Groups[3].Value == KIND_ERROR, int.Parse(EntryMatch.Groups[4].Value)));
            }
        }

        /* A count may be broken over two lines of the comment, so it is looked for in the comment as one. */
        string Flattened = Regex.Replace(string.Join(" ", Lines.Select(line => line.TrimStart(' ', '/', '*'))),
            @"\s+", " ");
        return new()
        {
            Entries = Entries,
            StatedFileCounts = ReadCounts(_fileCountPattern.Match(Flattened)),
            StatedDirectoryCounts = ReadCounts(_directoryCountPattern.Match(Flattened))
        };
    }


    // Private static methods.
    private static (int Errors, int Warnings)? ReadCounts(Match match)
    {
        if (!match.Success)
        {
            return null;
        }
        int Warnings = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        return (int.Parse(match.Groups[1].Value), Warnings);
    }
}
