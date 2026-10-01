namespace KeigValCompiler.Semantician.Binding;

/* What a name on its own was found to stand for. */
internal enum SimpleNameMeaningKind
{
    /* A local of a block around the name. */
    Local,

    /* A parameter of the function, or a record's positional parameter in a member's starting value. */
    Parameter,

    /* A generic parameter of the function or of a type around the name. */
    GenericParameter,

    /* A keyword standing for a library type, as "int". */
    KeywordType,

    /* What member lookup found in a type around the name. */
    TypeMember,

    /* What a namespace holds: a field, property, function or event, found at a namespace level or an import. */
    NameSpaceMember,

    /* A type a namespace holds, at a namespace level or an import. */
    NameSpaceType,

    NameSpace,

    /* More than one of the namespaces the file imports offering the name. */
    ImportAmbiguity,

    NotFound
}
