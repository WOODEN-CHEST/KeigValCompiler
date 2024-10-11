using KeigValCompiler.Semantician.Member.Class;
using KeigValCompiler.Semantician.Member.Function.Statement;

namespace KeigValCompiler.Semantician.Member.Field;

internal class PackField : PackMember
{
    // Internal fields.
    internal Identifier Type { get; private init; }
    internal ReturnableValueStatement? InitialValue { get; private init; }


    // Constructors.
    internal PackField(string identifier,
        PackMemberModifiers modifiers,
        PackClass packClass,
        Identifier type,
        ReturnableValueStatement? initialValue)
        : base(identifier, modifiers, packClass)
    {
        Type = type ?? throw new ArgumentNullException(nameof(modifiers));
        InitialValue = initialValue;
        VerifyDefinition();
    }

    internal PackField(string identifier,
        PackMemberModifiers modifiers,
        string nameSpace,
        PackSourceFile sourceFile,
        Identifier type,
        ReturnableValueStatement initialValue)
        : base(identifier, modifiers, nameSpace, sourceFile)
    {
        Type = type ?? throw new ArgumentNullException(nameof(modifiers));
        InitialValue = initialValue;
        VerifyDefinition();
    }


    // Private methods.
    private void VerifyDefinition()
    {
        PackMemberModifiers[] AllowedModifiers = new PackMemberModifiers[]
        {
            PackMemberModifiers.Private,
            PackMemberModifiers.Protected,
            PackMemberModifiers.Public,
            PackMemberModifiers.Static,
            PackMemberModifiers.Readonly,
            PackMemberModifiers.BuiltIn
        };
        if (HasAnyModifiersExcept(AllowedModifiers))
        {
            throw new PackContentException("Fields may only have the following modifiers:" +
                $" {string.Join(", ", AllowedModifiers.Select(Modifier => Modifier.ToString().ToLower()))}");
        }
    }
}