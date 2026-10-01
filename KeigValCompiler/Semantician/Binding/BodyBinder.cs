using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Bound;
using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Resolver;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* Binds one body into the bound tree: a function's, an accessor's, a constructor's or an operator's, or the
 * starting value of a field or a property. It holds what the names in the body depend on: the member the body
 * belongs to, which names are seen from, the type around it and whether there is an object for "this" to name,
 * what the body returns, and the locals and parameters in scope as binding goes.
 *
 * Messages about the body are reported at the line of the statement or value they are about, in the member's
 * file. Where a name could mean two things, as in C#'s "Color Color" rule, one meaning is bound with its messages
 * held back, to be reported only if that meaning is the one kept. */
internal sealed class BodyBinder
{
    // Internal fields.
    internal BodyBindingContext Context { get; private init; }
    internal PackResolutionContext Resolution => Context.Resolution;

    /* The function, field or property the body belongs to. */
    internal PackMember Member { get; private init; }

    /* The function, or null for a starting value. */
    internal PackFunction? Function { get; private init; }

    /* The nearest type around the body, as its own declaration sees itself, as Thing<T> inside Thing<T>, or null
     * for a body a namespace holds. */
    internal DeclaredType? ContainingType { get; private init; }

    /* Whether there is no object for "this" to name: in a static member, in what a namespace holds, which
     * belongs to no object either, in an operator, and in a constant's value. */
    internal bool IsStatic { get; private init; }

    /* Whether "this" names a structure's value which the code cannot change: in the members of a readonly
     * structure other than its constructors and "init" accessors, and in a member or an accessor written
     * readonly, as in C#. */
    internal bool IsThisReadOnly { get; private init; }

    /* A field's or property's starting value, worked out before the object it belongs to is made. */
    internal bool IsInitializer { get; private init; }

    /* What the function returns, or null when it returns nothing; the error type when its type did not
     * resolve, which has been reported. */
    internal SemanticType? ReturnType { get; private init; }

    /* A function whose body has "yield" in it, which makes it an iterator. */
    internal bool IsIterator { get; private init; }

    internal LocalScope? Scope { get; private set; }
    internal NameBinder Names { get; private init; }
    internal ExpressionBinder Expressions { get; private init; }
    internal StatementBinder Statements { get; private init; }


    // Private fields.
    /* Where the body's messages go: the resolution's, or while a meaning which may be dropped is bound, a
     * collection of their own. */
    private CompilerMessageCollection _messages;


    // Constructors.
    internal BodyBinder(BodyBindingContext context, PackMember member)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Member = member ?? throw new ArgumentNullException(nameof(member));
        Function = member as PackFunction;
        _messages = context.Resolution.Messages;

        PackMember? TypeAround = GetTypeAround(member);
        ContainingType = (TypeAround == null) ? null : Resolution.TypeReader.GetInstanceType(TypeAround);
        PackMember Owner = MemberRelations.IsAccessor(member) ? MemberRelations.GetHoldingMember(member)! : member;
        IsStatic = (TypeAround == null) || MemberRelations.IsWrittenStatic(Owner)
            || (MemberRelations.GetOperatorOverload(member) != null);
        IsThisReadOnly = !IsStatic && (TypeAround is PackStruct) && (Function != null)
            && (Function is not PackConstructor) && !IsInitAccessor(Function)
            && (TypeAround.HasModifier(PackMemberModifiers.Readonly)
                || Function.HasModifier(PackMemberModifiers.Readonly)
                || (!ReferenceEquals(Owner, Function) && Owner.HasModifier(PackMemberModifiers.Readonly)));
        IsInitializer = member is PackField or PackProperty;
        ReturnType = (Function == null) ? null : ReadReturnType(Function);
        IsIterator = (Function?.Statements != null)
            && Function.Statements.Any(statement => ContainsYield(statement));

        Names = new(this);
        Expressions = new(this);
        Statements = new(this);
        Scope = CreateParameterScope();
    }


    // Private static methods.
    /* Whether a statement has "yield" in it, not counting what a lambda in it holds. */
    private static bool ContainsYield(Statement statement)
    {
        return (statement is YieldStatement)
            || ((statement is not LambdaStatement) && statement.Children.Any(child => ContainsYield(child)));
    }

    /* The nearest type holding a member, past the property or indexer holding an accessor. */
    private static PackMember? GetTypeAround(PackMember member)
    {
        PackMember? Holder = MemberRelations.GetHoldingMember(member);
        while ((Holder != null) && !MemberRelations.IsType(Holder))
        {
            Holder = MemberRelations.GetHoldingMember(Holder);
        }
        return Holder;
    }

    private static bool IsInitAccessor(PackFunction function)
    {
        return MemberRelations.IsAccessor(function)
            && (function.SelfIdentifier.SourceCodeName == KGVL.KEYWORD_INIT);
    }


    // Internal methods.
    /* The function's body, as a block of its own. A function returning nothing written with "=>" runs the value
     * instead of returning it. */
    internal BoundBlock BindFunctionBody()
    {
        if (Function?.Statements == null)
        {
            throw new InvalidOperationException($"\"{Member}\" has no body to bind.");
        }
        return Statements.BindFunctionBody(Function.Statements, Function.IsExpressionBodied);
    }

    /* A field's starting value, converted to the field's type. */
    internal BoundExpression BindFieldInitializer(PackField field)
    {
        ArgumentNullException.ThrowIfNull(field, nameof(field));
        return BindInitializer(field.InitialValue!, field.Type);
    }

    /* A property's starting value, converted to the property's type. */
    internal BoundExpression BindPropertyInitializer(PackProperty property)
    {
        ArgumentNullException.ThrowIfNull(property, nameof(property));
        return BindInitializer(property.InitialValue!, property.Type);
    }

    /* The value a constant is given, which has to be known at compile time. Null when it is not, which is
     * reported, or when the value has errors, which have been. As in C#, a constant of a reference type other
     * than string can only be null, which is reported apart, since its value may well be a constant of its own. */
    internal ConstantValue? GetConstantInitializerValue(BoundExpression value, string constantName,
        Statement where)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));
        ArgumentNullException.ThrowIfNull(constantName, nameof(constantName));
        ArgumentNullException.ThrowIfNull(where, nameof(where));

        if (value.HasErrors)
        {
            return null;
        }
        if ((value.ConstantValue == null) && (value is BoundConversion { Operand.ConstantValue: not null })
            && (value.Type != null) && !value.Type.Equals(GetLibraryType(LibraryTypes.String)))
        {
            AddError(Resolution.ErrorCreator.ConstantReferenceNotNull.CreateOptions(constantName,
                value.Type.ToString()), where);
        }
        else if (value.ConstantValue == null)
        {
            AddError(Resolution.ErrorCreator.ConstantValueNotConstant.CreateOptions(constantName), where);
        }
        return value.ConstantValue;
    }

    /* Messages about a body point at the line of what they are about, or the member's, for what the parser made
     * without one. */
    internal void AddError(ErrorCreateOptions error, Statement where)
    {
        ArgumentNullException.ThrowIfNull(where, nameof(where));
        AddError(error, where.Origin);
    }

    internal void AddError(ErrorCreateOptions error, SourceFileOrigin origin)
    {
        GetTarget(origin).AddError(error);
    }

    /* Binds something with its messages held back rather than reported, giving them, so that the caller can
     * report them, if the meaning bound is the one kept, or drop them. */
    internal T HoldMessages<T>(Func<T> bind, out CompilerMessageCollection held)
    {
        ArgumentNullException.ThrowIfNull(bind, nameof(bind));

        CompilerMessageCollection Previous = _messages;
        held = new();
        _messages = held;
        try
        {
            return bind();
        }
        finally
        {
            _messages = Previous;
        }
    }

    internal void ReportHeld(CompilerMessageCollection held)
    {
        ArgumentNullException.ThrowIfNull(held, nameof(held));

        foreach (CompilerMessage Message in held)
        {
            _messages.Add(Message);
        }
    }

    /* A type written in the body, resolved where it is written, and reported there when it cannot be; the error
     * type then. */
    internal SemanticType ReadType(TypeTargetIdentifier type, Statement where)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(where, nameof(where));

        Context.TypeNames.Resolve(type, Member, GetTarget(where.Origin));
        return Resolution.TypeReader.Read(type) ?? ErrorType.Instance;
    }

    /* A type written in the body and resolved, checked as C# checks one, anywhere in it: no array of a static
     * class, no static class as a type argument, and every type argument satisfying the constraints of its
     * parameter. A mistake is reported about what kind and name describe, at the line of where. Whether there was
     * one. Whether the whole type may be a static class depends on where it is written, and is the caller's to
     * check. */
    internal bool CheckWrittenType(TypeTargetIdentifier type, string kind, string name, Statement where)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));
        ArgumentNullException.ThrowIfNull(kind, nameof(kind));
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(where, nameof(where));

        bool IsReported = false;
        Action<ErrorCreateOptions> Report = error =>
        {
            AddError(error, where);
            IsReported = true;
        };
        StaticClassUsageChecker.CheckWrittenType(type, kind, name, Resolution.ErrorCreator, Report);
        ConstraintChecker.CheckTypeArguments(type, Resolution, Report);
        return IsReported;
    }

    /* A type a declaration names, resolved with the declaration, as seen inside it: the error type when it did
     * not resolve, which has been reported. */
    internal SemanticType ReadDeclaredType(TypeTargetIdentifier? type)
    {
        return ((type == null) ? null : Resolution.TypeReader.Read(type)) ?? ErrorType.Instance;
    }

    /* A type the compiler knows by name, or the error type when the library does not declare it, which has been
     * reported. */
    internal SemanticType GetLibraryType(LibraryType libraryType)
    {
        ArgumentNullException.ThrowIfNull(libraryType, nameof(libraryType));

        PackMember? Declaration = Resolution.Registry.GetDeclaredType(libraryType);
        return (Declaration == null) ? ErrorType.Instance : Resolution.TypeReader.GetInstanceType(Declaration);
    }

    /* The types around the body, innermost first, as each declaration sees itself. */
    internal IEnumerable<PackMember> GetTypesAround()
    {
        for (PackMember? Type = GetTypeAround(Member); Type != null; Type = GetTypeAround(Type))
        {
            yield return Type;
        }
    }

    /* A function's own generic parameter of a name, or null. */
    internal GenericTypeParameter? FindFunctionGenericParameter(string name)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        return Function?.GenericParameters.FirstOrDefault(
            parameter => parameter.SelfIdentifier.SourceCodeName == name);
    }

    internal BlockScope PushBlock()
    {
        BlockScope Block = new(Scope);
        Scope = Block;
        return Block;
    }

    internal void PopScope(LocalScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope, nameof(scope));

        if (!ReferenceEquals(scope, Scope))
        {
            throw new InvalidOperationException("Scopes are left in the order they were entered.");
        }
        Scope = scope.Parent;
    }


    // Private methods.
    private BoundExpression BindInitializer(Statement value, TypeTargetIdentifier type)
    {
        return Expressions.BindConvertedValue(value, ReadDeclaredType(type));
    }

    /* Messages about a body point at the line of what they are about, or the member's, for what the parser made
     * without one. */
    private MessageTarget GetTarget(SourceFileOrigin origin)
    {
        int Line = (origin.Line != CompilerMessageLocation.LINE_UNKNOWN) ? origin.Line
            : Member.SourceFileOrigin.Line;
        return new(_messages, new(Member.SourceFile.Path, Line, CompilerMessageLocation.COLUMN_UNKNOWN));
    }

    /* The parameters the body can name: the function's own, or for an accessor its indexer's, with a setter's
     * "value". In a record's members' starting values, its positional parameters are seen too, as in C#, though
     * only an instance member's starting value can use them. */
    private ParameterScope CreateParameterScope()
    {
        ParameterScope Parameters = new(null);
        if (Function == null)
        {
            if ((MemberRelations.GetHoldingMember(Member) is PackClass Record)
                && Record.HasModifier(PackMemberModifiers.Record))
            {
                PackConstructor? Primary = Record.Functions.OfType<PackConstructor>()
                    .FirstOrDefault(constructor => constructor.IsPrimary);
                AddParameters(Parameters, Primary?.Parameters, !IsStatic);
            }
            return Parameters;
        }

        if (!MemberRelations.IsAccessor(Function))
        {
            AddParameters(Parameters, Function.Parameters, true);
            return Parameters;
        }

        IPackAccessorHolder Holder = (IPackAccessorHolder)MemberRelations.GetHoldingMember(Function)!;
        AddParameters(Parameters, (Holder as PackIndexer)?.Parameters, true);
        if (!ReferenceEquals(Function, Holder.GetFunction))
        {
            Function.ValueParameter ??= new(Holder.Type, new Identifier(KGVL.KEYWORD_VALUE),
                FunctionParameterModifier.None);
            Parameters.Add(Function.ValueParameter, ReadDeclaredType(Holder.Type), true);
        }
        return Parameters;
    }

    private void AddParameters(ParameterScope scope, FunctionParameterCollection? parameters, bool isUsable)
    {
        foreach (FunctionParameter Parameter in parameters ?? Enumerable.Empty<FunctionParameter>())
        {
            scope.Add(Parameter, ReadDeclaredType(Parameter.Type), isUsable);
        }
    }

    /* A getter returns its property's or indexer's type, and any other accessor, as a constructor, nothing. */
    private SemanticType? ReadReturnType(PackFunction function)
    {
        if (MemberRelations.IsAccessor(function))
        {
            IPackAccessorHolder Holder = (IPackAccessorHolder)MemberRelations.GetHoldingMember(function)!;
            return ReferenceEquals(function, Holder.GetFunction) ? ReadDeclaredType(Holder.Type) : null;
        }
        return ((function is PackConstructor) || (function.ReturnType == null)) ? null
            : ReadDeclaredType(function.ReturnType);
    }
}
