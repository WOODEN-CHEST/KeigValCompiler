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

    /* What is written before the name and a '.', a namespace or a type, as the "KGVL.Collections" of
     * "KGVL.Collections.List<int>" or the "Outer<int>" of "Outer<int>.Inner". It is itself a name which may
     * have type arguments and a qualifier of its own, but never array levels or '?'. Null for a name written
     * on its own. */
    internal TypeTargetIdentifier? Qualifier { get; init; }

    /* The type through whose bases the name was found, when the type it names is declared in one of them:
     * the qualifier's type, for a name after one, as in "Derived.Inner", or else the type around where this
     * is written, as "Inner" is inside a class deriving from Outer<int>. The type model reads the type
     * holding it from there, since it is that base as seen from that type. Null otherwise. */
    internal PackMember? InheritedThrough { get; set; }

    /* The bases written on the way from InheritedThrough to the type declaring the one named, each written
     * by the type before it, as the lookup went through them, which the type model reads the type holding
     * it along. Null when the name was not found through bases, or the way could not be read, as through a
     * base whose type arguments did not resolve. */
    internal IReadOnlyList<TypeTargetIdentifier>? InheritedPath { get; set; }

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

    /* Whether the type is written as an array or with a '?' at any level, as opposed to only by its name
     * and type arguments, whatever those are written as. */
    internal bool IsArrayOrNullable => IsArray || _nullabilityByLevel.Any(isNullable => isNullable);

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

    /* The type as its resolved names spell it, or as source code does where one is not resolved. A resolved
     * name holds its namespace already, so a qualifier naming a namespace is left out, and the type reads the
     * same however it was written; one naming a type is kept, since its type arguments are part of which
     * type is meant, as in "Outer<int>.Inner" and "Outer<string>.Inner". */
    internal string FormatResolved()
    {
        return Format(true);
    }


    // Private methods.
    /* The type with its qualifier, type arguments, array levels and nullable markers in the order source
     * code has them. */
    private string Format(bool isResolved)
    {
        string? ResolvedName = isResolved ? MainTarget.ResolvedName : null;
        bool IsQualifierWritten = (Qualifier != null)
            && ((ResolvedName == null) || (Qualifier.MainTarget.Target is PackMember));

        StringBuilder Builder = new();
        if (IsQualifierWritten)
        {
            Builder.Append(Qualifier!.Format(isResolved)).Append(KGVL.NAMESPACE_SEPARATOR);
        }
        Builder.Append(((ResolvedName == null) || IsQualifierWritten) ? MainTarget.SourceCodeName : ResolvedName);
        if (TypeArguments.Length > 0)
        {
            Builder.Append(KGVL.GENERIC_TYPE_START)
                .Append(string.Join(", ", TypeArguments.Select(argument => argument.Format(isResolved))))
                .Append(KGVL.GENERIC_TYPE_END);
        }

        for (int Level = 0; Level < _nullabilityByLevel.Length; Level++)
        {
            if (Level > 0)
            {
                Builder.Append(KGVL.OPEN_SQUARE_BRACKET).Append(KGVL.CLOSE_SQUARE_BRACKET);
            }
            if (_nullabilityByLevel[Level])
            {
                Builder.Append(KGVL.TYPE_NULLABLE_INDICATOR);
            }
        }
        return Builder.ToString();
    }


    // Inherited methods.
    /* The type as source code writes it, as in "KGVL.Collections.List<int?>[]". */
    public override string ToString()
    {
        return Format(false);
    }
}
