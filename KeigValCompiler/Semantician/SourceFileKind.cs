namespace KeigValCompiler.Semantician;

/* Where a source file was read from. It decides what the file may contain, such as "builtin" members,
 * so it comes from where the compiler found the file and never from anything written in it. */
internal enum SourceFileKind
{
    /* The code being compiled. */
    User,

    /* The standard library, which the compiler reads before the user's code. */
    Library
}
