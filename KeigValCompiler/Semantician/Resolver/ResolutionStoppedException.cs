namespace KeigValCompiler.Semantician.Resolver;

/* Thrown by a pass which cannot go on, once it has reported why, to stop resolution there: what the passes
 * after it would report could only follow from what stopped it. FullPackResolver catches it. */
internal class ResolutionStoppedException : Exception
{
    // Constructors.
    internal ResolutionStoppedException() : base("Resolution was stopped, and the reason reported.") { }
}
