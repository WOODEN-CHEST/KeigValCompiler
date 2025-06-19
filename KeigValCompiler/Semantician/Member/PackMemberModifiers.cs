namespace KeigValCompiler.Semantician.Member;

[Flags]
internal enum PackMemberModifiers
{
    None = 0,
    Static = 1 << 0,

    Readonly = 1 << 1,

    BuiltIn = 1 << 2,

    Abstract = 1 << 3,
    Virtual = 1 << 4,
    Override = 1 << 5,

    Private = 1 << 6,
    Protected = 1 << 7,
    Public = 1 << 8,

    Inline = 1 << 9,

    Sealed = 1 << 10,

    Required = 1 << 11,

    Record = 1 << 12
}