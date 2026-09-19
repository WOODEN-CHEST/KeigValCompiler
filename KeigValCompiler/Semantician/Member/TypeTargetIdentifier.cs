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

    /* Nullability of every level of the type, innermost first: index 0 is the element type and
     * index ArrayRank the outermost array. Always holds exactly ArrayRank + 1 entries, because a
     * type of array depth N has N + 1 independently nullable positions. */
    internal IReadOnlyList<bool> NullabilityByLevel => _nullabilityByLevel;

    /* Array depth, where 0 means the type is not an array. 1 is "int[]" and 2 is "int[][]". */
    internal int ArrayRank => _nullabilityByLevel.Length - 1;
    internal bool IsArray => ArrayRank > 0;

    /* Nullability of the type taken as a whole, which for an array means the outermost array, as
     * in the "int[]?" case. Equal to IsElementNullable when the type is not an array. */
    internal bool IsNullable
    {
        get => _nullabilityByLevel[^1];
        set => _nullabilityByLevel[^1] = value;
    }

    /* Nullability of the innermost element, the "int" of "int?[][]". */
    internal bool IsElementNullable
    {
        get => _nullabilityByLevel[0];
        set => _nullabilityByLevel[0] = value;
    }


    // Private fields.
    /* A non array type still has one entry, for the type itself. */
    private bool[] _nullabilityByLevel = new bool[1];


    // Constructors.
    internal TypeTargetIdentifier(string sourceCodeName) : this(new Identifier(sourceCodeName)) { }

    internal TypeTargetIdentifier(Identifier mainTarget) : this(mainTarget, null) { }

    internal TypeTargetIdentifier(Identifier mainTarget,  TypeTargetIdentifier[]? typeArguments)
    {
        MainTarget = mainTarget ?? throw new ArgumentNullException(nameof(mainTarget));
        TypeArguments = typeArguments ?? Array.Empty<TypeTargetIdentifier>();
    }


    // Methods.
    internal bool GetNullabilityAtLevel(int level)
    {
        return _nullabilityByLevel[level];
    }

    internal void SetNullabilityAtLevel(int level, bool isNullable)
    {
        _nullabilityByLevel[level] = isNullable;
    }

    /* Replaces the entire nullability ladder, which also sets the array rank. The first entry is
     * the element type and each entry after it is one array level further out. */
    internal void SetNullabilityByLevel(IEnumerable<bool> nullabilityInnermostFirst)
    {
        ArgumentNullException.ThrowIfNull(nullabilityInnermostFirst, nameof(nullabilityInnermostFirst));

        bool[] Levels = nullabilityInnermostFirst.ToArray();
        if (Levels.Length == 0)
        {
            throw new ArgumentException("A type needs at least one nullability level, " +
                "for the element type itself.", nameof(nullabilityInnermostFirst));
        }

        _nullabilityByLevel = Levels;
    }
}
