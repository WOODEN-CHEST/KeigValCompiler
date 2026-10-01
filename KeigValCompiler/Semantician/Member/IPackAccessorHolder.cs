namespace KeigValCompiler.Semantician.Member;

/* A property or an indexer, whose value is read and set through accessors, each kept as a function so that
 * a body written for it has somewhere to live. "set" and "init" both set the value, the second only while an
 * object is created, so as in C# a member has at most one of the two. */
internal interface IPackAccessorHolder : IIdentifiable
{
    // Fields.
    /* The type of the value the accessors read and set. */
    TypeTargetIdentifier Type { get; }
    PackFunction? GetFunction { get; set; }
    PackFunction? SetFunction { get; set; }
    PackFunction? InitFunction { get; set; }
}
