namespace KeigValCompiler.Semantician.Member;

[Flags]
internal enum IdentifierType
{
    None = 0,
    Class = 1,
    Interface = 2,
    Field = 4,
    Property = 8,
    Function = 16,
    LocalField = 32
}