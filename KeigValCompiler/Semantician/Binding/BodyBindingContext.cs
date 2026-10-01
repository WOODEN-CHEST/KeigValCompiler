using KeigValCompiler.Semantician.Bound;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Resolver;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* What binding every body of one resolution shares: the resolution's context, the pieces built on it, which
 * look types, members and conversions up, and the starting values of fields. A constant field's value is
 * needed wherever the field is named, which may be before the pass binding every body reaches it, so a field's
 * starting value is bound when first needed, and once. */
internal sealed class BodyBindingContext
{
    // Static fields.
    /* How deep working out one constant may need others worked out first, each waiting on the next. Real
     * programs stay far below it; far enough past it, the waiting constants would run out of stack. */
    internal const int MAX_CONSTANT_DEPTH = 100;


    // Internal fields.
    internal PackResolutionContext Resolution { get; private init; }

    /* What derives from what, once every base list is resolved, as bodies see it. */
    internal IBaseTypeSource Bases { get; private init; }

    /* Resolves the types written in bodies. */
    internal TypeNameResolver TypeNames { get; private init; }
    internal MemberLookup Members { get; private init; }
    internal ConversionClassifier Conversions { get; private init; }


    // Private fields.
    /* The fields whose starting values are being bound, which a constant needing its own value comes back to. */
    private readonly HashSet<PackField> _fieldsBeingBound = new(ReferenceEqualityComparer.Instance);


    // Constructors.
    internal BodyBindingContext(PackResolutionContext resolution)
    {
        Resolution = resolution ?? throw new ArgumentNullException(nameof(resolution));
        Bases = new ResolvedBaseTypes();
        TypeNames = new(resolution, Bases);
        Members = new(resolution, Bases);
        Conversions = new(resolution);
    }


    // Internal methods.
    /* Binds a field's starting value, unless that has been done. A constant field whose value needs its own,
     * through the constants its value names, is reported here, once, and has no value. Constants waiting on
     * each other deeper than the limit stop resolution, having reported it, as base lists do. */
    internal void BindFieldInitializer(PackField field)
    {
        ArgumentNullException.ThrowIfNull(field, nameof(field));

        if ((field.InitialValue == null) || (field.BoundInitialValue != null))
        {
            return;
        }
        if (!_fieldsBeingBound.Add(field))
        {
            Resolution.AddError(Resolution.ErrorCreator.CircularConstant.CreateOptions(
                field.SelfIdentifier.SourceCodeName), field);
            return;
        }
        if (_fieldsBeingBound.Count > MAX_CONSTANT_DEPTH)
        {
            Resolution.AddError(Resolution.ErrorCreator.ConstantsTooDeep.CreateOptions(
                MemberRelations.GetQualifiedDisplayName(field), MAX_CONSTANT_DEPTH), field);
            throw new ResolutionStoppedException();
        }

        /* A constant of a type no constant can have has been reported, and is given no value. */
        BodyBinder Binder = new(this, field);
        field.BoundInitialValue = Binder.BindFieldInitializer(field);
        if (field.HasModifier(PackMemberModifiers.Const)
            && (Resolution.TypeReader.Read(field.Type) is SemanticType Type)
            && FieldTypeChecker.IsConstantType(Type))
        {
            field.ConstantValue = Binder.GetConstantInitializerValue(field.BoundInitialValue,
                field.SelfIdentifier.SourceCodeName, field.InitialValue);
        }
        _fieldsBeingBound.Remove(field);
    }

    /* A constant field's value, binding its starting value first if that is not done yet. Null when it is no
     * constant, or its value could not be had, which has been reported. */
    internal ConstantValue? GetConstantValue(PackField field)
    {
        ArgumentNullException.ThrowIfNull(field, nameof(field));

        if (!field.HasModifier(PackMemberModifiers.Const))
        {
            return null;
        }
        BindFieldInitializer(field);
        return field.ConstantValue;
    }
}
