using KeigValCompiler.Semantician.Member.Code;

namespace KeigValCompiler.Semantician.Member;

/* One constant of an enum: its name and, when one was written, the expression after its '='. The number
 * it stands for is not known while parsing, because that expression may name other constants, and a
 * constant without one takes the value after the previous constant's. The validation stage works the
 * numbers out, in declaration order. */
internal class PackEnumerationConstant
{
    // Fields.
    internal string Name { get; }

    /* Null when the constant has no '=' of its own. */
    internal Statement? ValueExpression { get; }
    internal SourceFileOrigin SourceFileOrigin { get; }


    // Constructors.
    internal PackEnumerationConstant(string name, Statement? valueExpression,
        SourceFileOrigin sourceFileOrigin)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ValueExpression = valueExpression;
        SourceFileOrigin = sourceFileOrigin;
    }
}
