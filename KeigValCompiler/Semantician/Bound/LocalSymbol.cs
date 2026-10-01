using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Bound;

/* A local variable or constant which a body declares, which the names in the body that mean it share. Its
 * type is known once its declaration is bound, which for one declared with "var" means once its starting
 * value is; until then it is the error type. */
internal sealed class LocalSymbol
{
    // Internal fields.
    internal string Name { get; private init; }
    internal SemanticType Type { get; set; } = ErrorType.Instance;

    /* Declared with "const", so that it holds a value fixed at compile time, which is ConstantValue once its
     * declaration is bound, or null when that value could not be had, which has been reported. */
    internal bool IsConstant { get; private init; }
    internal ConstantValue? ConstantValue { get; set; } = null;

    /* The statement declaring it, for the line messages about it point at. */
    internal Statement Declaration { get; private init; }


    // Constructors.
    internal LocalSymbol(string name, bool isConstant, Statement declaration)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsConstant = isConstant;
        Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
    }


    // Inherited methods.
    public override string ToString()
    {
        return Name;
    }
}
