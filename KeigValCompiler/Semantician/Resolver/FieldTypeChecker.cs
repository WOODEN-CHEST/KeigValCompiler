using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Checks what fields can hold, against C#'s rules. A constant's type is one whose values can be fixed when
 * the code is compiled: a number, a char, a bool, a string, an enum, or another reference type, whose only
 * constant is null. And no structure contains itself: a structure holds the values of its instance fields,
 * and of its properties storing their own values, directly, so no field or property of a structure has a
 * type which holds the structure, however indirectly, and each which does is reported, as in C#. A nullable
 * value holds the value it wraps, and a generic structure holds what stands for each of its generic
 * parameters it holds a value of. Which structures hold which is worked out by declaration rather than by
 * each type made from one, since those can grow without end, as S<S<T>> inside S<T> does. */
internal class FieldTypeChecker : IPackResolver
{
    // Static fields.
    private const string SEPARATOR = ", ";

    private static readonly LibraryType[] _constantLibraryTypes = LibraryTypes.Integers.Concat(new LibraryType[]
    {
        LibraryTypes.Char, LibraryTypes.Decimal, LibraryTypes.Boolean, LibraryTypes.String
    }).ToArray();


    // Private methods.
    private void CheckConstantType(PackField field, PackResolutionContext context)
    {
        if (!field.HasModifier(PackMemberModifiers.Const)
            || (context.TypeReader.Read(field.Type) is not SemanticType Type))
        {
            return;
        }

        bool IsConstantType = (Type is DeclaredType Declared)
            && (_constantLibraryTypes.Contains(Declared.LibraryType) || (Declared.Declaration is PackEnumeration)
                || (Declared.Declaration is PackClass or PackInterface or PackDelegate));
        if (!IsConstantType)
        {
            context.AddError(context.ErrorCreator.ConstantTypeNotConstant.CreateOptions(
                field.SelfIdentifier.SourceCodeName, Type.ToString()), field);
        }
    }

    /* Each stored member of a structure whose type holds the structure is reported, with the members
     * followed from the structure back to it. */
    private void CheckLayout(PackStruct structure,
        Dictionary<PackStruct, List<(PackMember Member, SemanticType Type)>> stored,
        Dictionary<PackStruct, HashSet<GenericTypeParameter>> heldParameters,
        PackResolutionContext context)
    {
        foreach ((PackMember Member, SemanticType Type) in stored[structure])
        {
            HashSet<PackStruct> Held = new(ReferenceEqualityComparer.Instance);
            AddHeldStructures(Type, heldParameters, Held);
            List<string>? Path = FindPath(Held, structure, stored, heldParameters);
            if (Path == null)
            {
                continue;
            }

            Path.Insert(0, GetStepName(structure, Member));
            context.AddError(context.ErrorCreator.StructContainsItself.CreateOptions(
                MemberRelations.GetKindName(Member), Member.SelfIdentifier.SourceCodeName,
                structure.SelfIdentifier.SourceCodeName, Type.ToString(), string.Join(SEPARATOR, Path)), Member);
        }
    }

    /* The members followed from one of the structures held to the target, each as "Structure.Member", or
     * null when none of them holds it. Empty when one of them is the target itself. */
    private List<string>? FindPath(HashSet<PackStruct> held,
        PackStruct target,
        Dictionary<PackStruct, List<(PackMember Member, SemanticType Type)>> stored,
        Dictionary<PackStruct, HashSet<GenericTypeParameter>> heldParameters)
    {
        if (held.Contains(target))
        {
            return new();
        }

        Dictionary<PackStruct, (PackStruct Holder, PackMember Member)?> Reached =
            new(ReferenceEqualityComparer.Instance);
        Queue<PackStruct> Pending = new();
        foreach (PackStruct Start in held)
        {
            Reached.Add(Start, null);
            Pending.Enqueue(Start);
        }

        while (Pending.Count > 0)
        {
            PackStruct Current = Pending.Dequeue();
            foreach ((PackMember Member, SemanticType Type) in stored.GetValueOrDefault(Current)
                ?? new List<(PackMember Member, SemanticType Type)>())
            {
                HashSet<PackStruct> Next = new(ReferenceEqualityComparer.Instance);
                AddHeldStructures(Type, heldParameters, Next);
                foreach (PackStruct Structure in Next.Where(structure => !Reached.ContainsKey(structure)))
                {
                    Reached.Add(Structure, (Current, Member));
                    if (ReferenceEquals(Structure, target))
                    {
                        return GetPath(target, Reached);
                    }
                    Pending.Enqueue(Structure);
                }
            }
        }
        return null;
    }

    private List<string> GetPath(PackStruct target,
        Dictionary<PackStruct, (PackStruct Holder, PackMember Member)?> reached)
    {
        List<string> Path = new();
        for ((PackStruct Holder, PackMember Member)? Step = reached[target]; Step != null;
            Step = reached[Step.Value.Holder])
        {
            Path.Insert(0, GetStepName(Step.Value.Holder, Step.Value.Member));
        }
        return Path;
    }

    private string GetStepName(PackStruct structure, PackMember member)
    {
        return structure.SelfIdentifier.SourceCodeName + KGVL.MEMBER_ACCESS + member.SelfIdentifier.SourceCodeName;
    }

    /* The structures a value of a type holds directly: the type itself if it is a structure, what a nullable
     * value wraps, and what stands for each generic parameter of a structure which that structure holds a
     * value of. */
    private void AddHeldStructures(SemanticType type,
        Dictionary<PackStruct, HashSet<GenericTypeParameter>> heldParameters,
        HashSet<PackStruct> held)
    {
        if (type is not DeclaredType Declared)
        {
            return;
        }
        if (Declared.IsNullable)
        {
            AddHeldStructures(Declared.TypeArguments[0], heldParameters, held);
            return;
        }
        if (Declared.Declaration is not PackStruct Structure)
        {
            return;
        }

        held.Add(Structure);
        TypeSubstitution Substitution = TypeSubstitution.Of(Declared);
        foreach (GenericTypeParameter Parameter in heldParameters.GetValueOrDefault(Structure)
            ?? new HashSet<GenericTypeParameter>())
        {
            SemanticType? Argument = Substitution.GetReplacement(Parameter);
            if (Argument != null)
            {
                AddHeldStructures(Argument, heldParameters, held);
            }
        }
    }

    /* Which generic parameters each structure holds a value of, its own or those of a type around it:
     * those its stored members are, or which a structure they are holds in turn. Each structure starts
     * holding none, and gains them until none does, since structures can hold each other. */
    private Dictionary<PackStruct, HashSet<GenericTypeParameter>> GetHeldParameters(
        Dictionary<PackStruct, List<(PackMember Member, SemanticType Type)>> stored)
    {
        Dictionary<PackStruct, HashSet<GenericTypeParameter>> Held = new(ReferenceEqualityComparer.Instance);
        foreach (PackStruct Structure in stored.Keys)
        {
            Held.Add(Structure, new(ReferenceEqualityComparer.Instance));
        }

        bool IsChanged = true;
        while (IsChanged)
        {
            IsChanged = false;
            foreach (KeyValuePair<PackStruct, List<(PackMember Member, SemanticType Type)>> Entry in stored)
            {
                foreach ((PackMember Member, SemanticType Type) in Entry.Value)
                {
                    HashSet<GenericTypeParameter> Parameters = new(ReferenceEqualityComparer.Instance);
                    AddHeldParameters(Type, Held, Parameters, new(ReferenceEqualityComparer.Instance));
                    foreach (GenericTypeParameter Parameter in Parameters)
                    {
                        IsChanged |= Held[Entry.Key].Add(Parameter);
                    }
                }
            }
        }
        return Held;
    }

    /* The generic parameters a value of a type holds a value of, as far as is known. */
    private void AddHeldParameters(SemanticType type,
        Dictionary<PackStruct, HashSet<GenericTypeParameter>> held,
        HashSet<GenericTypeParameter> parameters,
        HashSet<SemanticType> followed)
    {
        if (type is GenericParameterType ParameterType)
        {
            parameters.Add(ParameterType.Parameter);
            return;
        }
        if ((type is not DeclaredType Declared) || !followed.Add(type))
        {
            return;
        }
        if (Declared.IsNullable)
        {
            AddHeldParameters(Declared.TypeArguments[0], held, parameters, followed);
            return;
        }
        if (Declared.Declaration is not PackStruct Structure)
        {
            return;
        }

        TypeSubstitution Substitution = TypeSubstitution.Of(Declared);
        foreach (GenericTypeParameter Parameter in held.GetValueOrDefault(Structure)
            ?? new HashSet<GenericTypeParameter>())
        {
            SemanticType? Argument = Substitution.GetReplacement(Parameter);
            if (Argument != null)
            {
                AddHeldParameters(Argument, held, parameters, followed);
            }
        }
    }

    /* What each structure stores in itself, and the types of those: its instance fields, and its instance
     * properties which store their own values, having an accessor without a body. A type which did not
     * resolve has been reported, and is left out. */
    private Dictionary<PackStruct, List<(PackMember Member, SemanticType Type)>> GetStoredMembers(
        PackResolutionContext context)
    {
        Dictionary<PackStruct, List<(PackMember Member, SemanticType Type)>> Stored =
            new(ReferenceEqualityComparer.Instance);
        foreach (PackStruct Structure in context.Pack.Structs)
        {
            IEnumerable<(PackMember Member, TypeTargetIdentifier Type)> Fields = Structure.Fields
                .Where(field => !field.HasAnyModifier(PackMemberModifiers.Static, PackMemberModifiers.Const))
                .Select(field => ((PackMember)field, field.Type));
            IEnumerable<(PackMember Member, TypeTargetIdentifier Type)> Properties = Structure.Properties
                .Where(property => !property.HasAnyModifier(PackMemberModifiers.Static,
                    PackMemberModifiers.Abstract, PackMemberModifiers.BuiltIn)
                    && property.SubMembers.Any(accessor => ((PackFunction)accessor).Statements == null))
                .Select(property => ((PackMember)property, property.Type));

            List<(PackMember Member, SemanticType Type)> Members = new();
            foreach ((PackMember Member, TypeTargetIdentifier Written) in Fields.Concat(Properties))
            {
                SemanticType? Type = context.TypeReader.Read(Written);
                if (Type != null)
                {
                    Members.Add((Member, Type));
                }
            }
            Stored.Add(Structure, Members);
        }
        return Stored;
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackField Field in context.Pack.Fields)
        {
            CheckConstantType(Field, context);
        }
        Dictionary<PackStruct, List<(PackMember Member, SemanticType Type)>> Stored = GetStoredMembers(context);
        Dictionary<PackStruct, HashSet<GenericTypeParameter>> HeldParameters = GetHeldParameters(Stored);
        foreach (PackStruct Structure in context.Pack.Structs)
        {
            CheckLayout(Structure, Stored, HeldParameters, context);
        }
    }
}
