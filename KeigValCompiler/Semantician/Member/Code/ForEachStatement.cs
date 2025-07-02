using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class ForEachStatement : Statement
{
    // Fields.
    internal Identifier? ElementType { get; set; } = null;
    internal Identifier ElementName { get; set; }
    internal Statement EnumeratorProvider { get; set; }


    // Constructors.
    public ForEachStatement(Identifier elementName, Statement enumeratorGetter)
    {
        ElementName = elementName;
        EnumeratorProvider = enumeratorGetter;
    }
}