using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* A conversion operator which applies to a user-defined conversion being classified, with the types it
 * converts between, as it is declared or lifted to the Nullables of its types, and for an interface's static
 * operator found through a generic parameter's constraints, that parameter. */
internal sealed record UserDefinedCandidate(PackFunction Method, SemanticType FromType, SemanticType ToType,
    bool IsLifted, GenericParameterType? ConstrainedTo);
