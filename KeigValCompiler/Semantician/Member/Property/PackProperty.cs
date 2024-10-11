using KeigValCompiler.Semantician.Member.Class;
using KeigValCompiler.Semantician.Member.Function;
using KeigValCompiler.Semantician.Member.Function.Statement;

namespace KeigValCompiler.Semantician.Member.Property;

internal class PackProperty : PackMember
{
    // Internal fields.
    internal Identifier Type { get; private init; }
    internal ReturnableValueStatement? InitialValue { get; private init; }
    internal PackFunction? GetFunction { get; private init; }
    internal PackFunction? SetFunction { get; private init; }
    internal PackFunction? InitFunction { get; private init; }


    // Constructors.
    internal PackProperty(string identifier,
        PackMemberModifiers modifiers,
        PackClass parentClass,
        Identifier type,
        PackFunction? getFunc,
        PackFunction? setFunc,
        PackFunction? initFunc,
        ReturnableValueStatement? defaultValue)
        : base(identifier, modifiers, parentClass)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        GetFunction = getFunc;
        SetFunction = setFunc;
        InitFunction = initFunc;
        InitialValue = defaultValue;

        VerifyDefinition();
    }

    internal PackProperty(string identifier,
        PackMemberModifiers modifiers,
        string parentNamespace,
        PackSourceFile sourceFile,
        Identifier type,
        PackFunction? getFunc,
        PackFunction? setFunc,
        PackFunction? initFunc,
        ReturnableValueStatement? defaultValue)
        : base(identifier, modifiers, parentNamespace, sourceFile)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        GetFunction = getFunc;
        SetFunction = setFunc;
        InitFunction = initFunc;
        InitialValue = defaultValue;

        VerifyDefinition();
    }


    // Private methods.
    private void VerifyDefinition()
    {
        int FunctionCount = (GetFunction != null ? 1 : 0) + (SetFunction != null ? 1 : 0) + (InitFunction != null ? 1 : 0);
        if (FunctionCount == 0)
        {
            throw new PackContentException($"Property member \"{FullyQualifiedIdentifier}\" must have at least one function.");
        }

        PackMemberModifiers[] AllowedModifiers = new PackMemberModifiers[]
        {
            PackMemberModifiers.Private,
            PackMemberModifiers.Protected,
            PackMemberModifiers.Public,
            PackMemberModifiers.Static,
            PackMemberModifiers.Abstract,
            PackMemberModifiers.Virtual,
            PackMemberModifiers.Override
        };
        if (HasAnyModifiersExcept(AllowedModifiers))
        {
            throw new PackContentException("Properties may only have the following modifiers:" +
                $" {string.Join(", ", AllowedModifiers.Select(Modifier => Modifier.ToString().ToLower()))}");
        }
    }
}