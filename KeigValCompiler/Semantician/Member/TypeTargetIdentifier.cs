using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal class TypeTargetIdentifier
{
    // Fields.
    internal Identifier MainTarget { get; set; }
    internal TypeTargetIdentifier[] TypeArguments { get; set; }
    internal bool IsNullable { get; set; } = false;

    /* Number of array dimensions applied to the type, where 0 means the type is not an array.
     * A rank of 1 is "int[]", a rank of 2 is "int[][]". */
    internal int ArrayRank { get; set; } = 0;
    internal bool IsArray => ArrayRank > 0;

    /* Whether the array's elements are nullable, as in "int?[]", as opposed to IsNullable which
     * refers to the array itself, as in "int[]?". Meaningless when ArrayRank is 0.
     * PROVISIONAL: whether KGVL actually distinguishes these two is not yet decided. */
    internal bool IsElementNullable { get; set; } = false;


    // Constructors.
    internal TypeTargetIdentifier(string sourceCodeName) : this(new Identifier(sourceCodeName)) { }

    internal TypeTargetIdentifier(Identifier mainTarget) : this(mainTarget, null) { }

    internal TypeTargetIdentifier(Identifier mainTarget,  TypeTargetIdentifier[]? typeArguments)
    {
        MainTarget = mainTarget ?? throw new ArgumentNullException(nameof(mainTarget));
        TypeArguments = typeArguments ?? Array.Empty<TypeTargetIdentifier>();
    }
}