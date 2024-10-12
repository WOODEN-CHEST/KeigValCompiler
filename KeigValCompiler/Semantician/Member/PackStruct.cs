namespace KeigValCompiler.Semantician.Member;

internal class PackStruct : PackMember
{
    // Internal fields.
    internal PackInterface[] ImplementedInterfaces
    {
        get => _implementedInterfaces;
        set => _implementedInterfaces = value?.ToArray() ?? throw new ArgumentNullException(nameof(value));
    }
    internal OperatorOverloadCollection OperatorOverloads { get; private set; }
    internal GenericTypeParameterCollection GenericParameters { get; private init; }
    internal Identifier[] ExtendedItems { get; private init; }

    internal PackClass[] Classes => _classes.ToArray();
    internal PackInterface[] Interfaces => _interfaces.ToArray();
    internal PackProperty[] Properties => _properties.ToArray();
    internal PackField[] Fields => _fields.ToArray();
    internal PackFunction[] Functions => _functions.ToArray();
    internal PackEnum[] Enums => _enums.ToArray();
    internal PackStruct[] Structs => _structs.ToArray();


    // Private fields.
    private PackInterface[] _implementedInterfaces = Array.Empty<PackInterface>();
    private readonly List<PackFunction> _functions = new();
    private readonly List<PackProperty> _properties = new();
    private readonly List<PackField> _fields = new();
    private readonly List<PackIndexer> _indexers = new();
    private readonly List<PackClass> _classes = new();
    private readonly List<PackInterface> _interfaces = new();
    private readonly List<PackEnum> _enums = new();
    private readonly List<PackStruct> _structs = new();


    // Constructors.
    internal PackStruct(Identifier identifier,
        PackMemberModifiers modifiers,
        PackSourceFile sourceFile,
        PackNameSpace nameSpace,
        Identifier parentItem,
        GenericTypeParameter[]? typeParameters,
        OperatorOverload[]? operatorOverloads,
        Identifier[]? extendedItems)
        : base(identifier, modifiers, sourceFile, nameSpace, parentItem)
    {
        ExtendedItems = extendedItems ?? Array.Empty<Identifier>();
        OperatorOverloads = new(operatorOverloads ?? Array.Empty<OperatorOverload>());
        GenericParameters = new(typeParameters ?? Array.Empty<GenericTypeParameter>());
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
}