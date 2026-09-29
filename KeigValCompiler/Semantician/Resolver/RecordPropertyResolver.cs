using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Gives each record a property for each of its positional parameters, as C# does: a public property with
 * "get" and "init", starting with the parameter's value. A record which declares a property or field of the
 * parameter's name keeps the value in that instead, and so does one which inherits a member of that name
 * declaring no generic parameters, unless it is an abstract property, which the property made for it
 * overrides. What keeps the value is an instance field, or an instance property declaring a "get" accessor
 * of any access, of the parameter's type, as C# checks it; anything else is reported, and so is an
 * inherited member keeping the value which one of the record's own hides. A declared member of any other
 * kind leaves the property to be made, for DeclarationNameChecker to report the two sharing a name, as C#
 * reports it. Base records are done before the records deriving from them, whose properties depend on
 * theirs. It runs once every member is named, and names what it makes. The type of a parameter whose type
 * did not resolve, which has been reported, is not compared. */
internal class RecordPropertyResolver : IPackResolver
{
    // Private methods.
    /* A record deriving from itself, reported by InheritanceChecker, is done once, like any other. */
    private void ResolveRecord(PackClass record, HashSet<PackClass> done, PackResolutionContext context)
    {
        if (!done.Add(record))
        {
            return;
        }
        DeclaredType Instance = context.TypeReader.GetInstanceType(record);
        if ((context.Hierarchy.GetBaseClass(Instance)?.Declaration is PackClass Base)
            && Base.HasModifier(PackMemberModifiers.Record))
        {
            ResolveRecord(Base, done, context);
        }

        PackConstructor? Constructor = record.Functions.OfType<PackConstructor>()
            .FirstOrDefault(constructor => constructor.IsPrimary);
        if (Constructor == null)
        {
            return;
        }

        /* A parameter repeating another's name has been reported, and gets nothing of its own. */
        HashSet<string> Names = new();
        foreach (FunctionParameter Parameter in Constructor.Parameters)
        {
            if ((Parameter.Type != null) && Names.Add(Parameter.SelfIdentifier.SourceCodeName))
            {
                ResolveParameter(Parameter, Parameter.Type, Constructor, record, Instance, context);
            }
        }
    }

    private void ResolveParameter(FunctionParameter parameter,
        TypeTargetIdentifier type,
        PackConstructor constructor,
        PackClass record,
        DeclaredType instance,
        PackResolutionContext context)
    {
        string Name = parameter.SelfIdentifier.SourceCodeName;
        SemanticType? Type = context.TypeReader.Read(type);

        PackMember? Declared = MemberRelations.GetSignedMembers(record).FirstOrDefault(
            member => (member is PackProperty or PackField)
                && (MemberRelations.GetExplicitInterface(member) == null)
                && (member.SelfIdentifier.SourceCodeName == Name));
        if (Declared != null)
        {
            if (!IsKeeping(Declared, instance, Type, context))
            {
                ReportMismatch(parameter, type, Declared, instance, Type, constructor, record, context);
            }
            return;
        }

        foreach (DeclaredType Base in context.Hierarchy.GetBaseClasses(instance))
        {
            PackMember? Inherited = MemberRelations.GetSignedMembers(Base.Declaration)
                .Concat(((IPackTypeHolder)Base.Declaration).Types)
                .FirstOrDefault(member => (member.SelfIdentifier.SourceCodeName == Name)
                    && (InheritedMembers.GetArity(member) == 0) && InheritedMembers.IsInheritable(member)
                    && InheritedMembers.IsVisibleFrom(member, record));
            if (Inherited == null)
            {
                continue;
            }

            if (!IsKeeping(Inherited, Base, Type, context))
            {
                ReportMismatch(parameter, type, Inherited, Base, Type, constructor, record, context);
            }
            else if ((Inherited is PackProperty) && MemberRelations.IsAbstract(Inherited))
            {
                AddProperty(Name, type, true, constructor, record, context);
            }
            else if (GetOwnNamedMembers(record, Name).Any())
            {
                context.AddError(context.ErrorCreator.HiddenPositionalMember.CreateOptions(
                    record.SelfIdentifier.SourceCodeName, Name, MemberRelations.GetKindName(Inherited),
                    Base.ToString()), constructor);
            }
            return;
        }
        AddProperty(Name, type, false, constructor, record, context);
    }

    /* Whether a member can keep a parameter's value, as C# decides it: an instance field, or an instance
     * property declaring a "get" accessor itself, of any access, of the parameter's type. A "const" counts
     * against a field only, since on anything else ModifierChecker takes it off and reports it. The holder
     * is the type declaring the member, as the record sees it. */
    private bool IsKeeping(PackMember member,
        DeclaredType holder,
        SemanticType? type,
        PackResolutionContext context)
    {
        if ((member is not (PackProperty or PackField)) || member.HasModifier(PackMemberModifiers.Static)
            || ((member is PackField) && member.HasModifier(PackMemberModifiers.Const)))
        {
            return false;
        }

        DeclaredSignature Signature = context.SignatureReader.Read(member, TypeSubstitution.Of(holder));
        if ((type != null) && (Signature.Type != null) && !type.Equals(Signature.Type))
        {
            return false;
        }
        return (member is PackField) || (InheritedMembers.GetAccessor(member, KGVL.KEYWORD_GET) != null);
    }

    /* The members of a record named like a parameter, which would hide an inherited one keeping its value:
     * any member other than a constructor, an operator or an explicit implementation, and any nested type,
     * whatever their numbers of generic parameters, as in C#. */
    private IEnumerable<PackMember> GetOwnNamedMembers(PackClass record, string name)
    {
        return MemberRelations.GetSignedMembers(record).Where(InheritedMembers.IsInheritable)
            .Concat(record.Types)
            .Where(member => member.SelfIdentifier.SourceCodeName == name);
    }

    /* The type is given as written when it did not resolve. */
    private void ReportMismatch(FunctionParameter parameter,
        TypeTargetIdentifier writtenType,
        PackMember member,
        DeclaredType holder,
        SemanticType? type,
        PackConstructor constructor,
        PackClass record,
        PackResolutionContext context)
    {
        context.AddError(context.ErrorCreator.RecordMemberMismatch.CreateOptions(
            record.SelfIdentifier.SourceCodeName, parameter.SelfIdentifier.SourceCodeName,
            MemberRelations.GetKindName(member), holder.ToString(), type?.ToString() ?? writtenType.ToString()),
            constructor);
    }

    /* The property is placed and named as a written one would be by the passes before this one. */
    private void AddProperty(string name,
        TypeTargetIdentifier type,
        bool isOverride,
        PackConstructor constructor,
        PackClass record,
        PackResolutionContext context)
    {
        PackProperty Property = new(new Identifier(name), type, record.SourceFile)
        {
            Modifiers = PackMemberModifiers.Public
                | (isOverride ? PackMemberModifiers.Override : PackMemberModifiers.None),
            SourceFileOrigin = constructor.SourceFileOrigin,
            InitialValue = new IdentifiableAccessStatement(name) { Origin = constructor.SourceFileOrigin },
            IsSynthesized = true
        };
        Property.GetFunction = CreateAccessor(KGVL.KEYWORD_GET, Property, constructor, record);
        Property.InitFunction = CreateAccessor(KGVL.KEYWORD_INIT, Property, constructor, record);
        Property.NameSpace = record.NameSpace;
        Property.ParentItem = record.SelfIdentifier;
        record.AddProperty(Property);
        new MemberIdentifierResolver().ResolveProperty(Property, context);
    }

    private PackFunction CreateAccessor(string keyword,
        PackProperty property,
        PackConstructor constructor,
        PackClass record)
    {
        return new(new Identifier(keyword), record.SourceFile)
        {
            SourceFileOrigin = constructor.SourceFileOrigin,
            NameSpace = record.NameSpace,
            ParentItem = property.SelfIdentifier
        };
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        HashSet<PackClass> Done = new(ReferenceEqualityComparer.Instance);
        foreach (PackClass Record in context.Pack.Classes.Where(
            type => type.HasModifier(PackMemberModifiers.Record)).ToArray())
        {
            ResolveRecord(Record, Done, context);
        }
    }
}
