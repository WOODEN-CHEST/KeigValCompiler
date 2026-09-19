using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

/* How a skip to a synchronisation point after an error ended. */
internal enum SyncPointKind
{
    /* The file ran out before anywhere worth resuming at was found. */
    None,

    /* Stopped on something belonging to the broken construct which was skipped over. It is consumed,
     * and parsing resumes after it. */
    Resume,

    /* Stopped on something belonging to the construct the parser is inside of, which means the list
     * of constructs being read has ended. It is left where it is for the caller to see. */
    Terminator
}