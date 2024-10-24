namespace KeigValCompiler.Semantician.Resolver;

internal interface IInternalContentProvider
{
    BuiltInTypeRegistry AddInternalContent(DataPack pack);
}