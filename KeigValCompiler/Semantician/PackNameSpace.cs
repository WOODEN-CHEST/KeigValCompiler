﻿using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician;

internal class PackNameSpace : IIdentifiable
{
    // Fields.
    public Identifier SelfIdentifier { get; private init; }


    // Internal fields.
    internal PackClass[] Classes => _classes.ToArray();
    internal PackInterface[] Interfaces => _interfaces.ToArray();
    internal PackProperty[] Properties => _properties.ToArray();
    internal PackField[] Fields => _fields.ToArray();
    internal PackFunction[] Functions => _functions.ToArray();
    internal PackEnum[] Enums => _enums.ToArray();
    internal PackStruct[] Structs => _structs.ToArray();


    // Private fields.
    private readonly List<PackClass> _classes = new();
    private readonly List<PackInterface> _interfaces = new();
    private readonly List<PackProperty> _properties = new();
    private readonly List<PackField> _fields = new();
    private readonly List<PackFunction> _functions = new();
    private readonly List<PackEnum> _enums = new();
    private readonly List<PackStruct> _structs = new();


    // Constructors.
    internal PackNameSpace(Identifier name)
    {
        SelfIdentifier = name ?? throw new ArgumentNullException(nameof(name));
    }


    // Internal methods.
    internal void AddClass(PackClass item)
    {
        _classes.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    internal void AddInterface(PackInterface item)
    {
        _interfaces.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    internal void AddProperty(PackProperty item)
    {
        _properties.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    internal void AddField(PackField item)
    {
        _fields.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    internal void AddFunction(PackFunction item)
    {
        _functions.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    internal void AddEnum(PackEnum item)
    {
        _enums.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    internal void AddStruct(PackStruct item)
    {
        _structs.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }


    // Inherited methods.
    public override int GetHashCode()
    {
        return SelfIdentifier.GetHashCode();
    }

    public override string ToString()
    {
        return SelfIdentifier.ToString();
    }

    public override bool Equals(object? obj)
    {
        if (obj is PackNameSpace NameSpace)
        {
            return SelfIdentifier.Equals(NameSpace?.SelfIdentifier);
        }
        return false;
    }
}