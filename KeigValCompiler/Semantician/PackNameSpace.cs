using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Function;
using System.Linq;

namespace KeigValCompiler.Semantician;

internal class PackNameSpace : IIdentifiable
{
    // Fields.
    public Identifier ItemIdentifier { get; private init; }


    // Internal fields.
    //internal PackClass[] Classes => _classes.ToArray();
    //internal PackInterface[] Interfaces => _interfaces.ToArray();
    //internal PackProperty[] Properties => _properties.ToArray();
    //internal PackField[] Fields => _fields.ToArray();
    internal PackFunction[] Functions => _functions.ToArray();
    //internal PackEnum[] Enums => _enums.ToArray();


    // Private fields.
    //private readonly List<PackClass> _classes = new();
    //private readonly List<PackInterface> _interfaces = new();
    //private readonly List<PackProperty> _properties = new();
    //private readonly List<PackField> _fields = new();
    private readonly List<PackFunction> _functions = new();
    //private readonly List<PackEnum> _enums = new();


    // Constructors.
    internal PackNameSpace(Identifier name)
    {
        ItemIdentifier = name ?? throw new ArgumentNullException(nameof(name));
    }


    // Internal methods.
    //internal void AddClass(PackClass item)
    //{
    //    if (_classes.Contains(item))
    //    {
    //        throw new PackContentException($"Class {item.SelfIdentifier.SourceCodeName}" +
    //            $" already exists in namespace {ItemIdentifier.SourceCodeName}");
    //    }
    //    _classes.Add(item ?? throw new ArgumentNullException(nameof(item)));
    //}

    //internal void AddInterface(PackInterface item)
    //{
    //    if (_interfaces.Contains(item))
    //    {
    //        throw new PackContentException($"Interface {item.SelfIdentifier.SourceCodeName}" +
    //            $" already exists in namespace {ItemIdentifier.SourceCodeName}");
    //    }
    //    _interfaces.Add(item ?? throw new ArgumentNullException(nameof(item)));
    //}

    //internal void AddProperty(PackProperty item)
    //{
    //    if (_properties.Contains(item))
    //    {
    //        throw new PackContentException($"Property {item.SelfIdentifier.SourceCodeName}" +
    //            $" already exists in namespace {ItemIdentifier.SourceCodeName}");
    //    }
    //    _properties.Add(item ?? throw new ArgumentNullException(nameof(item)));
    //}

    //internal void AddField(PackField item)
    //{
    //    if (_fields.Contains(item))
    //    {
    //        throw new PackContentException($"Field {item.SelfIdentifier.SourceCodeName}" +
    //            $" already exists in namespace {ItemIdentifier.SourceCodeName}");
    //    }
    //    _fields.Add(item ?? throw new ArgumentNullException(nameof(item)));
    //}

    internal void AddFunction(PackFunction item)
    {
        if (_functions.Contains(item))
        {
            throw new PackContentException($"Function {item.SelfIdentifier.SourceCodeName}" +
                $" already exists in namespace {ItemIdentifier.SourceCodeName}");
        }
        _functions.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    //internal void AddEnum(PackEnum item)
    //{
    //    if (_enums.Contains(item))
    //    {
    //        throw new PackContentException($"Enum {item.SelfIdentifier.SourceCodeName}" +
    //            $" already exists in namespace {ItemIdentifier.SourceCodeName}");
    //    }
    //    _enums.Add(item ?? throw new ArgumentNullException(nameof(item)));
    //}


    // Inherited methods.
    public override int GetHashCode()
    {
        return ItemIdentifier.GetHashCode();
    }

    public override string ToString()
    {
        return ItemIdentifier.ToString();
    }

    public override bool Equals(object? obj)
    {
        if (obj is PackNameSpace NameSpace)
        {
            return ItemIdentifier.Equals(NameSpace?.ItemIdentifier);
        }
        return false;
    }
}