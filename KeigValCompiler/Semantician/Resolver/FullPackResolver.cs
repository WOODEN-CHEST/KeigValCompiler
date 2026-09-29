namespace KeigValCompiler.Semantician.Resolver;

/* Resolves declarations, binds the standard library, and checks declarations against C#'s rules.
 * Function bodies are not resolved yet. */
internal class FullPackResolver : IPackResolver
{
    // Inherited methods.
    public void ResolvePack(PackResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        /* The order must NOT be changed: each pass relies on what the ones before it set. Types are
         * named before any is looked up, known types are found before keywords can resolve through
         * them, and signatures are resolved before members can be named after their parameter types
         * or matched against what the compiler implements. Records get their properties last, since
         * which to make depends on the members they inherit. */
        new NameSpaceResolver().ResolvePack(context);
        new ParentItemResolver().ResolvePack(context);
        new TypeDeclarationResolver().ResolvePack(context);
        new KnownTypeResolver().ResolvePack(context);
        new SignatureResolver().ResolvePack(context);
        new MemberIdentifierResolver().ResolvePack(context);
        new LibraryBindingResolver().ResolvePack(context);
        new RecordPropertyResolver().ResolvePack(context);

        /* The checks need every member named and every type in a declaration resolved. ModifierChecker
         * comes first, since it takes off each modifier a member cannot have, so that the others see only
         * what the member may have and do not report again what follows from a wrong modifier.
         * InheritedConstraintResolver comes before the checks which compare types, since they read the
         * constraints overrides and explicit implementations take from what they override or implement.
         * Otherwise their order only decides the order of messages found on the same line. */
        new ModifierChecker().ResolvePack(context);
        new ImportChecker().ResolvePack(context);
        new MemberPlacementChecker().ResolvePack(context);
        new MemberBodyChecker().ResolvePack(context);
        new OperatorDeclarationChecker().ResolvePack(context);
        new InheritanceChecker().ResolvePack(context);
        new DeclarationNameChecker().ResolvePack(context);
        new InheritedConstraintResolver().ResolvePack(context);
        new DuplicateSignatureChecker().ResolvePack(context);
        new OperatorTypeChecker().ResolvePack(context);
        new OverrideChecker().ResolvePack(context);
        new InterfaceImplementationChecker().ResolvePack(context);
        new ConstraintChecker().ResolvePack(context);
        new FieldTypeChecker().ResolvePack(context);
        new StaticClassUsageChecker().ResolvePack(context);
        new AccessibilityChecker().ResolvePack(context);
    }
}
