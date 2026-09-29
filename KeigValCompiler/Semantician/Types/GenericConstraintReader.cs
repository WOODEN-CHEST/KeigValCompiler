using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Types;

/* The constraints a generic parameter is under, as types. Most are the ones written on it. A function which
 * overrides another or implements one explicitly cannot write its generic parameters' constraints, as in C#,
 * and each of its parameters is under the constraints of the other function's parameter at its position,
 * once InheritedConstraintResolver has said which that is. Only "class" and "struct" can be written on such a
 * parameter, to say what a '?' on it means, and whether they agree with the inherited constraints is for
 * ConstraintChecker to check. */
internal class GenericConstraintReader
{
    // Private fields.
    private readonly SemanticTypeReader _reader;

    /* Keyed by the object, since GenericTypeParameter compares by name and constraints. */
    private readonly Dictionary<GenericTypeParameter,
        (GenericTypeParameter Source, TypeSubstitution Substitution)> _sources =
        new(ReferenceEqualityComparer.Instance);


    // Constructors.
    internal GenericConstraintReader(SemanticTypeReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }


    // Internal methods.
    /* Gives a parameter the constraints of another. The substitution gives, for the generic parameters the
     * other's constraints are written in terms of, the types they are for this parameter. */
    internal void SetSource(GenericTypeParameter parameter,
        GenericTypeParameter source,
        TypeSubstitution substitution)
    {
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(substitution, nameof(substitution));

        _sources[parameter] = (source, substitution);
    }

    /* The parameter another takes its constraints from, or null for one under its own. */
    internal GenericTypeParameter? GetSource(GenericTypeParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));
        return _sources.TryGetValue(parameter,
            out (GenericTypeParameter Source, TypeSubstitution Substitution) Link) ? Link.Source : null;
    }

    internal bool HasSpecialConstraint(GenericTypeParameter parameter, SpecialGenericConstraint constraint)
    {
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));

        GenericTypeParameter Holder = GetConstraintHolder(parameter);
        return Holder.Constraints.Any(written => written.SpecialConstraint == constraint);
    }

    /* The types a parameter is constrained to, in terms of the parameter's own declaration. One which did not
     * resolve has been reported, and is left out. */
    internal IEnumerable<SemanticType> GetTypeConstraints(GenericTypeParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));

        List<TypeSubstitution> Substitutions = new();
        GenericTypeParameter Holder = GetConstraintHolder(parameter, Substitutions);
        foreach (GenericConstraint Constraint in Holder.Constraints.Where(
            constraint => constraint.ConstrainedItemName != null))
        {
            SemanticType? Type = _reader.Read(Constraint.ConstrainedItemName!);
            if (Type == null)
            {
                continue;
            }

            /* The substitutions were gathered from the parameter towards the one holding the constraints, so
             * they apply in the other order. */
            for (int Index = Substitutions.Count - 1; Index >= 0; Index--)
            {
                Type = Type.Substitute(Substitutions[Index]);
            }
            yield return Type;
        }
    }


    // Private methods.
    private GenericTypeParameter GetConstraintHolder(GenericTypeParameter parameter)
    {
        return GetConstraintHolder(parameter, new());
    }

    /* The parameter whose written constraints hold for this one, following one function's parameter to the
     * one it takes its constraints from, which may take them from another in turn. */
    private GenericTypeParameter GetConstraintHolder(GenericTypeParameter parameter,
        List<TypeSubstitution> substitutions)
    {
        HashSet<GenericTypeParameter> Followed = new(ReferenceEqualityComparer.Instance) { parameter };
        GenericTypeParameter Current = parameter;
        while (_sources.TryGetValue(Current,
            out (GenericTypeParameter Source, TypeSubstitution Substitution) Link) && Followed.Add(Link.Source))
        {
            substitutions.Add(Link.Substitution);
            Current = Link.Source;
        }
        return Current;
    }
}
