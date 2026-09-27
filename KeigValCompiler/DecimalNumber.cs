using KeigValCompiler.Semantician;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler;

/* A decimal literal as written, without its suffix, and the value TwoIntDecimal parsed from it. A
 * malformed literal keeps its text and has the value NaN, since an error was reported for it. */
internal record DecimalNumber(string Number, TwoIntDecimal Value);