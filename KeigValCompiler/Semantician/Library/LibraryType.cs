namespace KeigValCompiler.Semantician.Library;

/* A type the compiler knows by name, because the language itself refers to it, or because the
 * standard library declares builtin members on it which the compiler implements. Each exists once,
 * in LibraryTypes, so two are the same type exactly when they are the same object. */
internal sealed class LibraryType
{
    // Internal fields.
    internal string NameSpace { get; private init; }
    internal string Name { get; private init; }
    internal string FullName => NameSpace + KGVL.NAMESPACE_SEPARATOR + Name;

    /* The keyword naming it, such as "int", or null for a type without one. */
    internal string? Keyword { get; private init; }

    /* The names its generic parameters have in the library. Signatures refer to them by position,
     * so only messages use these. */
    internal IReadOnlyList<string> GenericParameterNames { get; private init; }


    // Constructors.
    internal LibraryType(string nameSpace, string name, string? keyword, params string[] genericParameterNames)
    {
        NameSpace = nameSpace ?? throw new ArgumentNullException(nameof(nameSpace));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Keyword = keyword;
        GenericParameterNames = genericParameterNames
            ?? throw new ArgumentNullException(nameof(genericParameterNames));
    }


    // Inherited methods.
    public override string ToString()
    {
        return FullName;
    }
}
