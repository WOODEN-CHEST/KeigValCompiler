using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal class PackDelegate : PackMember, IPackType, IGenericParameterHolder
{
    // Fields.
    public IEnumerable<PackFunction> Constructors => Enumerable.Empty<PackFunction>();


    // Internal fields.
    internal TypeTargetIdentifier? ReturnType { get; set; } = null;
    internal FunctionParameterCollection Parameters { get; } = new();
    public GenericTypeParameterCollection GenericParameters { get; } = new();


    // Constructors.
    public PackDelegate(Identifier identifier, TypeTargetIdentifier? returnType, PackSourceFile sourceFile)
        : base(identifier, sourceFile)
    {
        ReturnType = returnType;
    }
}