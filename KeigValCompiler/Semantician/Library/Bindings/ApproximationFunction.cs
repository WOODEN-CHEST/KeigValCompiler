namespace KeigValCompiler.Semantician.Library.Bindings;

/* One of decimal's functions which approximate: its name, the operation it is, and its parameters
 * without the iteration count its second overload adds. */
internal sealed class ApproximationFunction
{
    // Internal fields.
    internal string Name { get; private init; }
    internal IntrinsicOperation Operation { get; private init; }
    internal IReadOnlyList<SignatureParameter> Parameters { get; private init; }


    // Constructors.
    internal ApproximationFunction(string name,
        IntrinsicOperation operation,
        params SignatureParameter[] parameters)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Operation = operation;
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }
}
