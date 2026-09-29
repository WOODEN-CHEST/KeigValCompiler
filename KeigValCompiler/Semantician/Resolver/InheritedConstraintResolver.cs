using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

/* Gives the generic parameters of each function which overrides another or implements one explicitly the
 * constraints of that function's, by position, as C# does, through the GenericConstraintReader, so that
 * every check after it reads them. It reports nothing: an override which overrides nothing, and an explicit
 * implementation which implements nothing, are OverrideChecker's and InterfaceImplementationChecker's to
 * report, and keep only what they have written. A type deriving from itself, reported by InheritanceChecker,
 * is left alone. */
internal class InheritedConstraintResolver : IPackResolver
{
    // Private methods.
    private void ResolveType(PackMember type, PackResolutionContext context)
    {
        if (type is not (PackClass or PackStruct or PackInterface))
        {
            return;
        }
        DeclaredType Instance = context.TypeReader.GetInstanceType(type);
        if (context.Hierarchy.IsInBaseCycle(Instance))
        {
            return;
        }

        foreach (PackFunction Function in ((IPackFunctionHolder)type).Functions.Where(
            function => function.GenericParameters.Count > 0))
        {
            if (Function.HasModifier(PackMemberModifiers.Override))
            {
                ResolveOverride(Function, type, Instance, context);
            }
            else if (Function.ExplicitInterface != null)
            {
                (DeclaredType Interface, DeclaredSignature Implemented)? Found =
                    InheritedMembers.FindExplicitlyImplemented(Function, context);
                if (Found != null)
                {
                    SetSources(Function, Found.Value.Implemented, TypeSubstitution.Of(Found.Value.Interface),
                        context);
                }
            }
        }
    }

    private void ResolveOverride(PackFunction function,
        PackMember type,
        DeclaredType instance,
        PackResolutionContext context)
    {
        DeclaredSignature Signature = context.SignatureReader.Read(function, new());
        if (!Signature.IsComplete)
        {
            return;
        }

        OverrideLookup Lookup = InheritedMembers.FindOverridden(Signature, instance, type, context);
        if ((Lookup.Overridden != null) && (Lookup.Holder != null))
        {
            SetSources(function, Lookup.Overridden, TypeSubstitution.Of(Lookup.Holder), context);
        }
    }

    /* The substitution gives the types of the type holding the other function as the function's own type
     * sees it, and gets the function's own generic parameters for the other's. */
    private void SetSources(PackFunction function,
        DeclaredSignature source,
        TypeSubstitution substitution,
        PackResolutionContext context)
    {
        (GenericTypeParameter Own, GenericTypeParameter Source)[] Pairs = function.GenericParameters
            .Zip(source.GenericParameters).ToArray();
        foreach ((GenericTypeParameter Own, GenericTypeParameter Source) in Pairs)
        {
            substitution.Add(Source, new GenericParameterType(Own, false));
        }
        foreach ((GenericTypeParameter Own, GenericTypeParameter Source) in Pairs)
        {
            context.Constraints.SetSource(Own, Source, substitution);
        }
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackMember Type in context.Pack.Types)
        {
            ResolveType(Type, context);
        }
    }
}
