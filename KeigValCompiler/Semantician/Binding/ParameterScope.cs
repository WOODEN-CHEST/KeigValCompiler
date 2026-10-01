using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* The parameters a body can name, with their types: those of the function it belongs to, or of the indexer an
 * accessor belongs to, and the "value" a setter is given; in the starting values of a record's members, its
 * positional parameters, which a static member's cannot use, though they find them, as in C#. A parameter whose
 * type did not resolve has the error type. */
internal sealed class ParameterScope : LocalScope
{
    // Private fields.
    private readonly Dictionary<string, (FunctionParameter Parameter, SemanticType Type, bool IsUsable)>
        _parameters = new();


    // Constructors.
    internal ParameterScope(LocalScope? parent) : base(parent) { }


    // Internal methods.
    /* A second parameter of one name has been reported, and the first stays the one the name finds. */
    internal void Add(FunctionParameter parameter, SemanticType type, bool isUsable)
    {
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        _parameters.TryAdd(parameter.SelfIdentifier.SourceCodeName, (parameter, type, isUsable));
    }

    internal bool TryFind(string name, out FunctionParameter? parameter, out SemanticType? type, out bool isUsable)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        bool IsFound = _parameters.TryGetValue(name,
            out (FunctionParameter Parameter, SemanticType Type, bool IsUsable) Found);
        parameter = IsFound ? Found.Parameter : null;
        type = IsFound ? Found.Type : null;
        isUsable = IsFound && Found.IsUsable;
        return IsFound;
    }
}
