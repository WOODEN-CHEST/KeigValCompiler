using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackProperty : PackMember
{
    // Internal fields.
    internal string Type { get; private init; }
    internal string? InitialValue { get; private init; }
    internal PackFunction? GetFunction { get; private init; }
    internal PackFunction? SetFunction { get; private init; }
    internal PackFunction? InitFunction { get; private init; }


    // Constructors.
    internal PackProperty(string identifier,
        PackMemberModifiers modifiers,
        PackClass parentClass,
        string type,
        PackFunction? getFunc,
        PackFunction? setFunc,
        PackFunction? initFunc,
        string? defaultValue)
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
        string type,
        PackFunction? getFunc,
        PackFunction? setFunc,
        PackFunction? initFunc,
        string? defaultValue)
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
    }
}