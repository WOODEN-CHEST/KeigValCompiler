using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

/* The single sink every compilation stage queues its errors and warnings into, so that a stage can
 * report everything it found rather than only the first thing that went wrong. One of these is
 * created per compilation and handed to every stage's context object. */
internal class CompilerMessageCollection : IEnumerable<CompilerMessage>
{
    // Fields.
    /* Past this many errors in a single file, whatever the parser is doing in that file has stopped
     * making sense and the rest of its messages would just be noise. */
    internal const int MAX_ERRORS_PER_FILE = 100;

    internal int ErrorCount { get; private set; } = 0;
    internal int WarningCount => _messages.Count - ErrorCount;
    internal int Count => _messages.Count;
    internal bool HasErrors => ErrorCount > 0;


    // Private fields.
    private readonly List<CompilerMessage> _messages = new();
    private readonly Dictionary<string, int> _errorCountByFilePath = new();


    // Methods.
    public void Add(CompilerMessage message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        if (IsRepeatOfPreviousMessage(message))
        {
            return;
        }

        _messages.Add(message);

        if (!message.IsError)
        {
            return;
        }

        ErrorCount++;
        if (message.Location.HasFilePath)
        {
            string FilePath = message.Location.FilePath!;
            _errorCountByFilePath.TryGetValue(FilePath, out int PreviousCount);
            _errorCountByFilePath[FilePath] = PreviousCount + 1;
        }
    }

    public void AddError(ErrorCreateOptions error, CompilerMessageLocation location, string? notes)
    {
        Add(new CompilerMessage(error, location, notes));
    }

    public void AddWarning(WarningCreateOptions warning, CompilerMessageLocation location, string? notes)
    {
        Add(new CompilerMessage(warning, location, notes));
    }

    public int GetErrorCount(string? filePath)
    {
        if (filePath == null)
        {
            return 0;
        }
        _errorCountByFilePath.TryGetValue(filePath, out int ErrorCountForFile);
        return ErrorCountForFile;
    }

    public bool IsErrorLimitReached(string? filePath)
    {
        return GetErrorCount(filePath) >= MAX_ERRORS_PER_FILE;
    }

    /* Messages are queued in the order the compiler stumbles across them, which is not the order a
     * person wants to read them in. OrderBy is a stable sort, so messages sharing a location keep
     * the order they were found in. */
    public CompilerMessage[] GetSortedMessages()
    {
        return _messages
            .OrderBy(Message => Message.Location.FilePath ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(Message => Message.Location.Line)
            .ThenBy(Message => Message.Location.Column)
            .ToArray();
    }


    // Private methods.
    /* The same error reported twice at the same spot means error recovery went in a circle rather
     * than that the code is wrong twice. */
    private bool IsRepeatOfPreviousMessage(CompilerMessage message)
    {
        if (_messages.Count == 0)
        {
            return false;
        }

        CompilerMessage PreviousMessage = _messages[^1];
        return (PreviousMessage.Definition == message.Definition)
            && (PreviousMessage.Message == message.Message)
            && (PreviousMessage.Location.FilePath == message.Location.FilePath)
            && (PreviousMessage.Location.Line == message.Location.Line)
            && (PreviousMessage.Location.Column == message.Location.Column);
    }


    // Inherited methods.
    public IEnumerator<CompilerMessage> GetEnumerator()
    {
        return _messages.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}