namespace KeigValCompiler.Semantician;

internal interface IStringIDProvider
{
    string GetNext();
    string Get(ulong id);
}