using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician.Resolver;

/* Matches every builtin member of the standard library against the signatures the compiler
 * implements, and gives each matched function, or accessor of a matched property or indexer, the
 * operation it is. The match runs both ways, so a builtin member nothing implements and an
 * implementation nothing declares are both reported. Also checks the rest of what "builtin" asks of
 * the library: a builtin member has no body, and a builtin type no instance fields. */
internal class LibraryBindingResolver : IPackResolver
{
    // Private methods.
    private void BindHolder(object holder, PackResolutionContext context)
    {
        if ((holder is PackMember HolderType) && HolderType.HasModifier(PackMemberModifiers.BuiltIn))
        {
            CheckBuiltInTypeFields(HolderType, context);
        }

        /* Nothing the compiler implements is a field or an event. */
        IEnumerable<PackMember> Fields = (holder as IPackFieldHolder)?.Fields ?? Enumerable.Empty<PackField>();
        IEnumerable<PackMember> Events = (holder as IPackEventHolder)?.Events ?? Enumerable.Empty<PackEvent>();
        IEnumerable<PackMember> FieldsAndEvents = Fields.Concat(Events);
        foreach (PackMember Unimplementable in FieldsAndEvents.Where(IsBuiltIn))
        {
            context.AddError(context.ErrorCreator.BuiltInMemberNotImplemented.CreateOptions(
                Unimplementable.SelfIdentifier.ResolvedName!), Unimplementable);
        }

        if (holder is IPackFunctionHolder FunctionHolder)
        {
            foreach (PackFunction Function in FunctionHolder.Functions.Where(IsBuiltIn))
            {
                BindFunction(Function, null, holder, context);
            }
            foreach (PackProperty Property in FunctionHolder.Properties.Where(IsBuiltIn))
            {
                BindProperty(Property, holder, context);
            }
            foreach (PackIndexer Indexer in FunctionHolder.Indexers.Where(IsBuiltIn))
            {
                BindIndexer(Indexer, holder, context);
            }
        }
        if (holder is IOperatorOverloadHolder OverloadHolder)
        {
            foreach (OperatorOverload Overload in OverloadHolder.OperatorOverloads.Where(
                overload => IsBuiltIn(overload.Function)))
            {
                BindFunction(Overload.Function, Overload.OverloadedOperator, holder, context);
            }
        }
        if (holder is IPackTypeHolder TypeHolder)
        {
            foreach (PackMember NestedType in TypeHolder.Types)
            {
                BindHolder(NestedType, context);
            }
        }
    }

    private void CheckBuiltInTypeFields(PackMember type, PackResolutionContext context)
    {
        if (type is not IPackFieldHolder FieldHolder)
        {
            return;
        }

        foreach (PackField Field in FieldHolder.Fields)
        {
            if (!Field.HasAnyModifier(PackMemberModifiers.Static, PackMemberModifiers.Const))
            {
                context.AddError(context.ErrorCreator.BuiltInTypeWithInstanceField.CreateOptions(
                    type.SelfIdentifier.ResolvedName!, Field.SelfIdentifier.SourceCodeName), Field);
            }
        }
    }

    private void BindFunction(PackFunction function,
        OverloadableOperator? overloadedOperator,
        object holder,
        PackResolutionContext context)
    {
        BuiltInSignatureReader? Reader = CreateReader(function, holder, context);
        if (Reader == null)
        {
            return;
        }
        MemberSignature? Signature = Reader.ReadFunction(function, overloadedOperator);
        function.Intrinsic = Match(Signature, Reader, function, function, context);
    }

    private void BindProperty(PackProperty property, object holder, PackResolutionContext context)
    {
        BuiltInSignatureReader? Reader = CreateReader(property, holder, context);
        if (Reader == null)
        {
            return;
        }

        if (property.GetFunction != null)
        {
            property.GetFunction.Intrinsic = Match(Reader.ReadPropertyAccessor(property, true), Reader,
                property.GetFunction, property, context);
        }
        if (property.SetFunction != null)
        {
            property.SetFunction.Intrinsic = Match(Reader.ReadPropertyAccessor(property, false), Reader,
                property.SetFunction, property, context);
        }
    }

    private void BindIndexer(PackIndexer indexer, object holder, PackResolutionContext context)
    {
        BuiltInSignatureReader? Reader = CreateReader(indexer, holder, context);
        if (Reader == null)
        {
            return;
        }

        if (indexer.GetFunction != null)
        {
            indexer.GetFunction.Intrinsic = Match(Reader.ReadIndexerAccessor(indexer, true), Reader,
                indexer.GetFunction, indexer, context);
        }
        if (indexer.SetFunction != null)
        {
            indexer.SetFunction.Intrinsic = Match(Reader.ReadIndexerAccessor(indexer, false), Reader,
                indexer.SetFunction, indexer, context);
        }
    }

    /* Null when the member is not declared in a type the compiler knows by name, which is reported. */
    private BuiltInSignatureReader? CreateReader(PackMember member, object holder, PackResolutionContext context)
    {
        LibraryType? DeclaringType = (holder is PackMember HolderType)
            ? context.Registry.GetLibraryType(HolderType) : null;
        if (DeclaringType == null)
        {
            context.AddError(
                context.ErrorCreator.BuiltInMemberOfUnknownType.CreateOptions(GetReadableName(member)), member);
            return null;
        }
        return new(context.Registry, DeclaringType, (PackMember)holder);
    }

    /* The operation the signature is bound to, or null after reporting why there is none. A builtin
     * member's body is checked here as well, on the function which would have it. */
    private IntrinsicOperation? Match(MemberSignature? signature,
        BuiltInSignatureReader reader,
        PackFunction function,
        PackMember member,
        PackResolutionContext context)
    {
        if (signature == null)
        {
            if (reader.UnknownType != null)
            {
                context.AddError(context.ErrorCreator.BuiltInMemberUsesUnknownType.CreateOptions(
                    GetReadableName(member), reader.UnknownType.ToString()), member);
            }
            return null;
        }

        if (function.Statements != null)
        {
            context.AddError(context.ErrorCreator.BuiltInMemberWithBody.CreateOptions(signature.ToString()),
                member);
        }

        IntrinsicOperation? Operation = context.BindingTable.MatchIntrinsic(signature);
        if (Operation == null)
        {
            context.AddError(context.ErrorCreator.BuiltInMemberNotImplemented.CreateOptions(signature.ToString()),
                member);
        }
        return Operation;
    }

    /* The member's name after whatever holds it, for messages about members whose signature could not
     * be read, and whose resolved name would show the compiler's internal spelling. */
    private string GetReadableName(PackMember member)
    {
        string HolderName = member.ParentItem?.ResolvedName ?? member.NameSpace.SelfIdentifier.ResolvedName!;
        return HolderName + KGVL.NAMESPACE_SEPARATOR + member.SelfIdentifier.SourceCodeName;
    }

    private bool IsBuiltIn(PackMember member)
    {
        return member.HasModifier(PackMemberModifiers.BuiltIn);
    }


    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        foreach (PackNameSpace NameSpace in context.Pack.NameSpaces)
        {
            BindHolder(NameSpace, context);
        }

        /* A library which already has errors leaves implementations unmatched only because of them, so
         * reporting those as well would bury the real errors under invented ones. */
        if (context.HasLibraryErrors())
        {
            return;
        }
        foreach (MemberSignature Unmatched in context.BindingTable.UnmatchedSignatures)
        {
            context.Messages.AddError(context.ErrorCreator.BuiltInImplementationNotDeclared.CreateOptions(
                Unmatched.ToString()), CompilerMessageLocation.None, null);
        }
    }
}
