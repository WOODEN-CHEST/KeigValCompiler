using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal abstract class PackMember
{
    // Internal fields.
    internal virtual string Identifier { get; set; }
    internal abstract string FullyQualifiedIdentifier { get; }
    internal virtual PackClass? ParentClass { get; set; }
    internal virtual string ParentNamespace { get; set; }
    internal virtual PackSourceFile SourceFile { get; set; }
    internal virtual DataPack Pack => SourceFile.Pack;
    internal virtual PackMemberModifiers Modifiers { get; set; }

    internal bool IsStatic => (Modifiers & PackMemberModifiers.Static) != 0;
    internal bool IsAbstract => (Modifiers & PackMemberModifiers.Abstract) != 0;



    // Constructors.
    internal PackMember(string identifier, PackClass parentClass, PackMemberModifiers modifiers)
    {
        Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        ParentClass = parentClass ?? throw new ArgumentNullException(nameof(parentClass));
        Modifiers = modifiers;
    }

    internal PackMember(string identifier, string parentNamespace, PackMemberModifiers modifiers, PackSourceFile sourceFile)
    {

    }
}