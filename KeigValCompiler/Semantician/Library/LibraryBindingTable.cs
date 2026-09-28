namespace KeigValCompiler.Semantician.Library;

/* Every member signature the compiler implements itself, and the operation each one is. It is filled
 * by the binding providers, then matched against the library's builtin members once their signatures
 * are resolved. The match runs both ways: a builtin member with no signature here has nothing to
 * implement it, and a signature no member matched implements something the library does not
 * declare. Either means the library and the compiler have drifted apart. */
internal class LibraryBindingTable
{
    // Internal fields.
    internal int Count => _operations.Count;
    internal IEnumerable<MemberSignature> Signatures => _operations.Keys;
    internal IEnumerable<MemberSignature> UnmatchedSignatures =>
        _operations.Keys.Where(signature => !_matchedSignatures.Contains(signature));


    // Private fields.
    private readonly Dictionary<MemberSignature, IntrinsicOperation> _operations = new();
    private readonly HashSet<MemberSignature> _matchedSignatures = new();


    // Internal methods.
    /* Binding one signature twice is a mistake in the compiler rather than in the library, so it
     * throws instead of being reported. */
    internal void AddIntrinsic(MemberSignature signature, IntrinsicOperation operation)
    {
        ArgumentNullException.ThrowIfNull(signature, nameof(signature));

        if (!_operations.TryAdd(signature, operation))
        {
            throw new ArgumentException($"The signature \"{signature}\" is bound twice, to "
                + $"{_operations[signature]} and to {operation}.", nameof(signature));
        }
    }

    /* The operation a builtin member's signature is bound to, or null if none is. A signature found
     * here counts as matched from then on. */
    internal IntrinsicOperation? MatchIntrinsic(MemberSignature signature)
    {
        ArgumentNullException.ThrowIfNull(signature, nameof(signature));

        if (!_operations.TryGetValue(signature, out IntrinsicOperation Operation))
        {
            return null;
        }
        _matchedSignatures.Add(signature);
        return Operation;
    }
}
