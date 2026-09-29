using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Types;

/* What a type derives from, given in terms of its own type arguments: its base classes, nearest first, and
 * every interface it implements, however indirectly. A class naming no base class derives from object, and
 * so do structures, enums and delegates; an interface derives from no class. A type deriving from itself
 * has been reported by InheritanceChecker, and is only followed until a type comes round again. A generic
 * parameter's constraints are read through the GenericConstraintReader, so that one which takes them from
 * another function's parameter has that one's. */
internal class TypeHierarchy
{
    // Private fields.
    private readonly SemanticTypeReader _reader;
    private readonly GenericConstraintReader _constraints;
    private readonly BuiltInTypeRegistry _registry;

    /* Whether each interface declaration met so far is on a cycle of interfaces deriving from each other. */
    private readonly Dictionary<PackMember, bool> _interfaceCycles = new(ReferenceEqualityComparer.Instance);


    // Constructors.
    internal TypeHierarchy(SemanticTypeReader reader,
        GenericConstraintReader constraints,
        BuiltInTypeRegistry registry)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }


    // Internal methods.
    /* The object type, or null when the library does not declare one, which has been reported. */
    internal DeclaredType? GetObjectType()
    {
        PackMember? Declaration = _registry.GetDeclaredType(LibraryTypes.Object);
        return (Declaration == null) ? null : _reader.GetInstanceType(Declaration);
    }

    /* The class a type derives from directly, or null for object itself and for an interface. */
    internal DeclaredType? GetBaseClass(DeclaredType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        PackMember? ObjectDeclaration = _registry.GetDeclaredType(LibraryTypes.Object);
        if ((type.Declaration is PackInterface) || ReferenceEquals(type.Declaration, ObjectDeclaration))
        {
            return null;
        }
        if (type.Declaration is PackClass)
        {
            DeclaredType? WrittenBase = _reader.GetWrittenBaseTypes(type)
                .FirstOrDefault(written => written.Declaration is PackClass);
            if (WrittenBase != null)
            {
                return WrittenBase;
            }
        }
        return GetObjectType();
    }

    /* Every class a type derives from, nearest first, ending with object. */
    internal IEnumerable<DeclaredType> GetBaseClasses(DeclaredType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        HashSet<PackMember> Seen = new(ReferenceEqualityComparer.Instance) { type.Declaration };
        for (DeclaredType? Base = GetBaseClass(type); (Base != null) && Seen.Add(Base.Declaration);
            Base = GetBaseClass(Base))
        {
            yield return Base;
        }
    }

    /* Whether following a type's base classes, or for an interface its base interfaces, comes round to a
     * type met before, which only a type deriving from itself can lead to. */
    internal bool IsInBaseCycle(DeclaredType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (type.Declaration is PackInterface)
        {
            return IsReachingInterfaceCycle(type);
        }

        HashSet<PackMember> Seen = new(ReferenceEqualityComparer.Instance) { type.Declaration };
        for (DeclaredType? Base = GetBaseClass(type); Base != null; Base = GetBaseClass(Base))
        {
            if (!Seen.Add(Base.Declaration))
            {
                return true;
            }
        }
        return false;
    }

    /* Every interface a type implements, directly, through its base classes, or through other interfaces,
     * each once; for an interface, every interface it derives from. An interface deriving from itself is
     * listed, but not what it derives from, as C# takes such an interface to derive from nothing. */
    internal IReadOnlyList<DeclaredType> GetInterfaces(DeclaredType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        List<DeclaredType> Interfaces = new();
        HashSet<SemanticType> Seen = new();
        foreach (DeclaredType Holder in new DeclaredType[] { type }.Concat(GetBaseClasses(type)))
        {
            foreach (DeclaredType Base in GetWrittenInterfaces(Holder))
            {
                AddInterface(Base, Interfaces, Seen);
            }
        }
        return Interfaces;
    }

    /* The interfaces a type lists among its bases, and every interface those derive from, but not those
     * of its base classes. */
    internal IReadOnlyList<DeclaredType> GetOwnInterfaces(DeclaredType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        List<DeclaredType> Interfaces = new();
        HashSet<SemanticType> Seen = new();
        foreach (DeclaredType Base in GetWrittenInterfaces(type))
        {
            AddInterface(Base, Interfaces, Seen);
        }
        return Interfaces;
    }

    /* Whether a type is another, or derives from it: has it among its base classes or interfaces. A
     * reference type derives from object, and a generic parameter from what its constraints name. */
    internal bool IsSameOrDerived(SemanticType type, SemanticType baseType)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(baseType, nameof(baseType));

        return IsSameOrDerived(type, baseType, new(ReferenceEqualityComparer.Instance));
    }

    /* Whether a type is another, or has it among its base classes, interfaces left out. A generic parameter
     * has the classes its constraints name, directly or through other generic parameters. */
    internal bool IsSameOrDerivedClass(SemanticType type, SemanticType baseType)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(baseType, nameof(baseType));

        if (type.Equals(baseType))
        {
            return true;
        }
        if (type is DeclaredType Declared)
        {
            return GetBaseClasses(Declared).Any(baseClass => baseClass.Equals(baseType));
        }
        return (type is GenericParameterType ParameterType)
            && GetConstraintClasses(ParameterType.Parameter).Any(
                constraintClass => IsSameOrDerivedClass(constraintClass, baseType));
    }

    /* Whether a type's values are references: a class, interface or delegate, an array, or a generic
     * parameter constrained to be one, with "class" or with a class other than object. As in C#, a
     * parameter constrained to another which says "class" is not known to be one, but one constrained to
     * another which names a class is. */
    internal bool IsReferenceType(SemanticType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (type is DeclaredType Declared)
        {
            return Declared.Declaration is PackClass or PackInterface or PackDelegate;
        }
        return (type is GenericParameterType ParameterType)
            && (_constraints.HasSpecialConstraint(ParameterType.Parameter, SpecialGenericConstraint.Class)
                || GetConstraintClasses(ParameterType.Parameter).Any());
    }

    /* Whether a type's values are values: a structure or an enum, or a generic parameter constrained with
     * "struct". */
    internal bool IsValueType(SemanticType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        if (type is GenericParameterType ParameterType)
        {
            return _constraints.HasSpecialConstraint(ParameterType.Parameter, SpecialGenericConstraint.Struct);
        }
        return type.IsValueType;
    }

    /* The classes other than object a generic parameter is constrained to, directly or through the generic
     * parameters it is constrained to, each once. */
    internal IReadOnlyList<DeclaredType> GetConstraintClasses(GenericTypeParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter, nameof(parameter));

        List<DeclaredType> Classes = new();
        AddConstraintClasses(parameter, Classes, new(ReferenceEqualityComparer.Instance));
        return Classes;
    }


    // Private methods.
    private IEnumerable<DeclaredType> GetWrittenInterfaces(DeclaredType type)
    {
        return _reader.GetWrittenBaseTypes(type).Where(written => written.Declaration is PackInterface);
    }

    private void AddInterface(DeclaredType type, List<DeclaredType> interfaces, HashSet<SemanticType> seen)
    {
        if (!seen.Add(type))
        {
            return;
        }
        interfaces.Add(type);
        if (IsOnInterfaceCycle(type.Declaration))
        {
            return;
        }
        foreach (DeclaredType Base in GetWrittenInterfaces(type))
        {
            AddInterface(Base, interfaces, seen);
        }
    }

    /* Whether an interface comes round to itself through what it derives from. */
    private bool IsOnInterfaceCycle(PackMember declaration)
    {
        if (_interfaceCycles.TryGetValue(declaration, out bool IsKnownCycle))
        {
            return IsKnownCycle;
        }

        HashSet<PackMember> Reached = new(ReferenceEqualityComparer.Instance);
        Queue<DeclaredType> Pending = new(GetWrittenInterfaces(_reader.GetInstanceType(declaration)));
        bool IsCycle = false;
        while (!IsCycle && (Pending.Count > 0))
        {
            DeclaredType Next = Pending.Dequeue();
            IsCycle = ReferenceEquals(Next.Declaration, declaration);
            if (!IsCycle && Reached.Add(Next.Declaration))
            {
                foreach (DeclaredType Base in GetWrittenInterfaces(Next))
                {
                    Pending.Enqueue(Base);
                }
            }
        }
        _interfaceCycles[declaration] = IsCycle;
        return IsCycle;
    }

    /* Whether an interface, or any interface it derives from, however indirectly, is on a cycle. */
    private bool IsReachingInterfaceCycle(DeclaredType type)
    {
        HashSet<PackMember> Reached = new(ReferenceEqualityComparer.Instance);
        Queue<DeclaredType> Pending = new(new DeclaredType[] { type });
        while (Pending.Count > 0)
        {
            DeclaredType Next = Pending.Dequeue();
            if (!Reached.Add(Next.Declaration))
            {
                continue;
            }
            if (IsOnInterfaceCycle(Next.Declaration))
            {
                return true;
            }
            foreach (DeclaredType Base in GetWrittenInterfaces(Next))
            {
                Pending.Enqueue(Base);
            }
        }
        return false;
    }

    /* The generic parameters already followed are kept, so that parameters constrained to each other,
     * reported by the checks of constraints, are not followed round forever. */
    private bool IsSameOrDerived(SemanticType type, SemanticType baseType, HashSet<GenericTypeParameter> followed)
    {
        if (type.Equals(baseType))
        {
            return true;
        }
        if (baseType.Equals(GetObjectType()) && IsReferenceType(type))
        {
            return true;
        }
        if (type is DeclaredType Declared)
        {
            return GetBaseClasses(Declared).Any(baseClass => baseClass.Equals(baseType))
                || GetInterfaces(Declared).Any(implemented => implemented.Equals(baseType));
        }
        if ((type is not GenericParameterType ParameterType) || !followed.Add(ParameterType.Parameter))
        {
            return false;
        }
        return _constraints.GetTypeConstraints(ParameterType.Parameter).Any(
            constraintType => IsSameOrDerived(constraintType, baseType, followed));
    }

    private void AddConstraintClasses(GenericTypeParameter parameter,
        List<DeclaredType> classes,
        HashSet<GenericTypeParameter> followed)
    {
        if (!followed.Add(parameter))
        {
            return;
        }

        DeclaredType? ObjectType = GetObjectType();
        foreach (SemanticType Constraint in _constraints.GetTypeConstraints(parameter))
        {
            if ((Constraint is DeclaredType Class) && (Class.Declaration is PackClass) && !Class.Equals(ObjectType)
                && !classes.Contains(Class))
            {
                classes.Add(Class);
            }
            else if (Constraint is GenericParameterType ConstraintParameter)
            {
                AddConstraintClasses(ConstraintParameter.Parameter, classes, followed);
            }
        }
    }
}
