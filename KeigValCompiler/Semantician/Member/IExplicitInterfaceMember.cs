namespace KeigValCompiler.Semantician.Member;

/* A member which can implement an interface's member explicitly, as in "int IFoo.Bar()": functions,
 * operators, properties and indexers. As in C#, such a member is reached only through the interface,
 * never by its own name, which is what lets a type have both "const int MaxValue" and the
 * "IMinMaxValue<int>.MaxValue" property its interface asks for. */
internal interface IExplicitInterfaceMember
{
    // Fields.
    /* The interface whose member this implements, or null for an ordinary member. */
    TypeTargetIdentifier? ExplicitInterface { get; set; }
}
