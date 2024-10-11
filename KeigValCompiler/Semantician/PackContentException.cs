namespace KeigValCompiler.Semantician;

internal class PackContentException : Exception
{
    internal PackContentException(string message) : base($"Invalid pack content! ${message}") { }
}