using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Types;

/* Which type stands for each of some generic parameters, as int stands for T in Thing<int>. A parameter
 * with no type given stays itself. */
internal sealed class TypeSubstitution
{
    // Private fields.
    /* Keyed by the object, since GenericTypeParameter compares by name and constraints. */
    private readonly Dictionary<GenericTypeParameter, SemanticType> _replacements =
        new(ReferenceEqualityComparer.Instance);


    // Internal static methods.
    /* The types a declared type gives its own generic parameters and those of each type around it, so
     * that what its declaration says in terms of those parameters can be said in terms of the type. */
    internal static TypeSubstitution Of(DeclaredType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        TypeSubstitution Substitution = new();
        for (DeclaredType? Current = type; Current != null; Current = Current.ContainingType)
        {
            if (Current.Declaration is not IGenericParameterHolder GenericsHolder)
            {
                continue;
            }

            int Position = 0;
            foreach (GenericTypeParameter Parameter in GenericsHolder.GenericParameters)
            {
                if (Position < Current.TypeArguments.Count)
                {
                    Substitution.Add(Parameter, Current.TypeArguments[Position]);
                }
                Position++;
            }
        }
        return Substitution;
    }

    /* The generic parameters of one function standing for those of another, by position, so that two
     * functions can be compared as C# compares them: "Foo<T>(T)" and "Foo<U>(U)" have the same signature. */
    internal static TypeSubstitution ByPosition(GenericTypeParameterCollection from,
        GenericTypeParameterCollection to)
    {
        ArgumentNullException.ThrowIfNull(from, nameof(from));
        ArgumentNullException.ThrowIfNull(to, nameof(to));

        TypeSubstitution Substitution = new();
        foreach ((GenericTypeParameter FromParameter, GenericTypeParameter ToParameter) in from.Zip(to))
        {
            Substitution.Add(FromParameter, new GenericParameterType(ToParameter, false));
        }
        return Substitution;
    }


    // Internal methods.
    internal void Add(GenericTypeParameter parameter, SemanticType replacement)
    {
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));
        _replacements[parameter] = replacement ?? throw new ArgumentNullException(nameof(replacement));
    }

    internal SemanticType? GetReplacement(GenericTypeParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));

        _replacements.TryGetValue(parameter, out SemanticType? Replacement);
        return Replacement;
    }
}
