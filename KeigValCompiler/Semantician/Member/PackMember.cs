using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal abstract class PackMember
{
    // Internal fields.
    internal virtual string Identifier { get; set; }
    internal virtual string FullyQualifiedIdentifier => ParentClass != null
        ? $"{ParentClass.FullyQualifiedIdentifier}.{Identifier}" : $"{NameSpace}.{Identifier}";
    internal virtual PackClass? ParentClass { get; set; }
    internal virtual string NameSpace { get; set; }
    internal virtual PackSourceFile SourceFile { get; set; }
    internal virtual DataPack Pack => SourceFile.Pack;
    internal virtual PackMemberModifiers Modifiers { get; set; }
    internal bool IsStatic => (Modifiers & PackMemberModifiers.Static) != 0;
    internal bool IsAbstract => (Modifiers & PackMemberModifiers.Abstract) != 0;



    // Constructors.
    internal PackMember(string identifier, PackMemberModifiers modifiers, PackClass parentClass)
        : this(identifier, modifiers)
    {
        ParentClass = parentClass ?? throw new ArgumentNullException(nameof(parentClass));
        NameSpace = ParentClass.NameSpace;
        SourceFile = ParentClass.SourceFile;
        VerifyModifiers();
    }

    internal PackMember(string identifier, PackMemberModifiers modifiers, string parentNamespace,  PackSourceFile sourceFile)
        : this(identifier, modifiers)
    {
        ParentClass = null;
        NameSpace = parentNamespace ?? throw new ArgumentNullException(nameof(parentNamespace));
        SourceFile = sourceFile ?? throw new ArgumentNullException(nameof(sourceFile));
        VerifyModifiers();
    }

    private PackMember(string identifier, PackMemberModifiers modifiers)
    {
        Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        Modifiers = modifiers;
    }


    // Internal static functions.
    internal int CountAccessModifiers()
    {
        return ((Modifiers & PackMemberModifiers.Private) > 0 ? 1 : 0)
            + ((Modifiers & PackMemberModifiers.Protected) > 0 ? 1 : 0)
            + ((Modifiers & PackMemberModifiers.Public) > 0 ? 1 : 0);
    }

    internal bool HasAnyModifier(params PackMemberModifiers[] targets)
    {
        foreach (PackMemberModifiers Target in targets)
        {
            if ((Modifiers & Target) > 0)
            {
                return true;
            }
        }

        return false;
    }

    internal bool HasAnyModifiersExcept(params PackMemberModifiers[] allowedModifiers)
    {
        PackMemberModifiers CombinedModifiers = PackMemberModifiers.None;
        foreach (PackMemberModifiers AllowedModifier in allowedModifiers)
        {
            CombinedModifiers |= AllowedModifier;
        }

        return (CombinedModifiers | Modifiers) != CombinedModifiers;
    }

    // Private methods.
    private void VerifyModifiers()
    {
        if ((ParentClass == null) && HasAnyModifier(PackMemberModifiers.Abstract, PackMemberModifiers.Virtual,
            PackMemberModifiers.Override, PackMemberModifiers.Static) && (this is not PackClass))
        {
            throw new PackContentException("Namespace members may not be abstract, virtual, overridden or static.");
        }
        if ((ParentClass?.IsStatic ?? false) && HasAnyModifier(PackMemberModifiers.Abstract, PackMemberModifiers.Virtual,
            PackMemberModifiers.Override))
        {
            throw new PackContentException("Members of static classes may not be abstract, virtual or overridden.");
        }
        if ((ParentClass?.IsStatic ?? false) && !HasAnyModifier(PackMemberModifiers.Static))
        {
            throw new PackContentException("Members of static classes must be static.");
        }
        if (HasAnyModifier(PackMemberModifiers.Virtual) && HasAnyModifier(PackMemberModifiers.Abstract))
        {
            throw new PackContentException("A member cannot be both abstract and virtual.");
        }
        if (HasAnyModifier(PackMemberModifiers.Override) && HasAnyModifier(PackMemberModifiers.Abstract))
        {
            throw new PackContentException("A member cannot be both abstract and overridden.");
        }
        if (HasAnyModifier(PackMemberModifiers.BuiltIn) && HasAnyModifier(PackMemberModifiers.Abstract,
            PackMemberModifiers.Virtual, PackMemberModifiers.Override))
        {
            throw new PackContentException("A built-in member cannot be both abstract, virtual or overridden.");
        }

        int AccessModifierCount = CountAccessModifiers();
        if (AccessModifierCount == 0)
        {
            Modifiers |= PackMemberModifiers.Private;
        }
        else if (AccessModifierCount > 1)
        {
            throw new PackContentException("Members may only have 1 access modifier.");
        }
    }


    // Inherited methods.
    public override int GetHashCode()
    {
        return FullyQualifiedIdentifier.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is PackClass)
        {
            return FullyQualifiedIdentifier == ((PackClass)obj)?.FullyQualifiedIdentifier;
        }
        return false;
    }

    public override string ToString()
    {
        return FullyQualifiedIdentifier;
    }
}