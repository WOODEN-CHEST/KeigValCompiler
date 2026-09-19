using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

internal abstract class AbstractParserBase
{
    // Protected fields.
    protected SourceDataParser Parser => Context.Parser;
    protected ParserUtilities Utils => Context.Utilities;
    protected PackSourceFile SourceFile => Context.SourceFile;
    protected ErrorRepository ErrorCreator => Context.ErrorCreator;
    protected CompilerMessageCollection Messages => Context.Messages;
    protected virtual PackParsingContext Context { get; private init; }


    // Constructors.
    internal AbstractParserBase(PackParsingContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }


    // Protected methods.
    protected CompilerMessageLocation GetCurrentLocation()
    {
        return new(Parser.FilePath, Parser.Line, Parser.GetColumn(Parser.DataIndex));
    }

    /* For errors where the parser still knows exactly where it is and what it is looking at - the
     * construct is understood, it is just wrong. Parsing carries straight on from the call. */
    protected void AddError(ErrorCreateOptions error)
    {
        Messages.AddError(error, GetCurrentLocation(), null);
    }

    protected void AddWarning(WarningCreateOptions warning)
    {
        AddWarning(warning, GetCurrentLocation());
    }

    protected void AddWarning(WarningCreateOptions warning, CompilerMessageLocation location)
    {
        Messages.AddWarning(warning, location, null);
    }

    /* For errors which left the parser with no idea where it is. The thrower unwinds to the nearest
     * loop over a repeatable construct, which calls this to queue the message and skip ahead to
     * somewhere the next one of those constructs could start. Returns false if the file ran out,
     * in which case the calling loop must stop.
     *
     * The caller must break out of its loop when it sees one of the terminatorChars, because those are
     * the one case where this returns without having moved the cursor. A loop which carries on anyway
     * would call this again at the same spot forever. */
    protected bool RecoverFromError(SourceFileReadException exception,
        char[] resumeChars,
        char[] terminatorChars)
    {
        ArgumentNullException.ThrowIfNull(exception, nameof(exception));
        ArgumentNullException.ThrowIfNull(resumeChars, nameof(resumeChars));
        ArgumentNullException.ThrowIfNull(terminatorChars, nameof(terminatorChars));

        Messages.Add(exception.CompilerMessage);
        if (Messages.IsErrorLimitReached(SourceFile.Path))
        {
            throw new SourceFileAbortException(SourceFile.Path);
        }

        SyncPointKind SyncPoint = Parser.SkipToSyncPoint(resumeChars, terminatorChars);
        if (SyncPoint == SyncPointKind.None)
        {
            return false;
        }

        if ((SyncPoint == SyncPointKind.Terminator) && terminatorChars.Contains(Parser.GetCharAtDataIndex()))
        {
            /* Left where it is on purpose - the calling loop is the one which needs to see it. */
            return true;
        }

        /* Either something parsing can resume after, or a closing bracket the caller has no use for
         * and which therefore belongs to nothing. Consuming it is also what guarantees the cursor
         * moved, without which the calling loop would come straight back here. */
        Parser.IncrementDataIndex();
        return true;
    }
}