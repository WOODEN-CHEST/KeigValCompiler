namespace KeigValCompiler.Semantician;

/* One "using" directive: the namespace it imports, and where it is written, so that a directive naming
 * a namespace which does not exist can be reported where it is. */
internal class NamespaceImport
{
    // Internal fields.
    internal PackNameSpace NameSpace { get; private init; }
    internal SourceFileOrigin Origin { get; private init; }


    // Constructors.
    internal NamespaceImport(PackNameSpace nameSpace, SourceFileOrigin origin)
    {
        NameSpace = nameSpace ?? throw new ArgumentNullException(nameof(nameSpace));
        Origin = origin;
    }
}
