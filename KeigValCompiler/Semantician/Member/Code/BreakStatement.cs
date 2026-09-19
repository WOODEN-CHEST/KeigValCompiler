using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class BreakStatement : Statement
{
    // Inherited fields.
    internal override IEnumerable<Statement> Children => Array.Empty<Statement>();
}