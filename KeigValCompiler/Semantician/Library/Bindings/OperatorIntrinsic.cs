using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Library.Bindings;

/* An operator, and the operation it is. */
internal sealed record OperatorIntrinsic(OverloadableOperator Operator, IntrinsicOperation Operation);
