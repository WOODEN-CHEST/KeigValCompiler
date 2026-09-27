using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler;

/* An integer literal: its digits as written, without prefix, separators or suffix, the value they make,
 * the base they are written in, and the type C# would give it from its value and suffix. Whether the
 * value fits the type it is assigned to is left to the validation stage, which knows that type. A
 * malformed literal keeps its digits and has the value zero, since an error was reported for it. */
internal record IntegerNumber(string Number, ulong Value, NumberBase Base, bool IsLong, bool IsUnsigned);