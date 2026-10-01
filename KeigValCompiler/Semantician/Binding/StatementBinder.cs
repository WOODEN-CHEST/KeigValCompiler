using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Bound;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Member.Code;
using KeigValCompiler.Semantician.Resolver;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Binding;

/* Binds the statements of a body. A block keeps its locals to itself, as in C#: each belongs to the whole block
 * declaring it, so that a name in the block finds it even before its declaration, where using it is an error,
 * and no local can have the name of another of the same block, or of a local or parameter of a block or
 * function around it. Kinds of statement not bound yet become BoundNotYetSupportedStatements, which count as
 * having errors, so that nothing in them is looked at or reported. */
internal sealed class StatementBinder
{
    // Private fields.
    private readonly BodyBinder _body;

    /* The local each declaration in the body declares, made when the block holding it is entered. */
    private readonly Dictionary<VariableAssignment, LocalSymbol> _locals = new(ReferenceEqualityComparer.Instance);

    /* The constant locals whose values are being bound, which a constant naming itself comes back to. */
    private readonly HashSet<LocalSymbol> _constantsBeingBound = new(ReferenceEqualityComparer.Instance);


    // Constructors.
    internal StatementBinder(BodyBinder body)
    {
        _body = body ?? throw new ArgumentNullException(nameof(body));
    }


    // Internal methods.
    /* A function's body, as a block of its own. A function returning nothing written with "=>" runs its value,
     * which has to be one that can stand as a statement, rather than return it. */
    internal BoundBlock BindFunctionBody(StatementCollection statements, bool isExpressionBodied)
    {
        ArgumentNullException.ThrowIfNull(statements, nameof(statements));

        if (isExpressionBodied && (_body.ReturnType == null) && (statements.Count == 1)
            && (statements[0] is ReturnStatement { ReturnValue: Statement Value }))
        {
            BlockScope Scope = _body.PushBlock();
            BoundStatement Run = BindExpressionStatement(Value);
            _body.PopScope(Scope);
            return new BoundBlock(null, Scope.Locals, new BoundStatement[] { Run });
        }
        return BindBlock(null, statements);
    }

    /* Whether a constant local's value is being bound, so that naming it there needs its own value. */
    internal bool IsConstantBeingBound(LocalSymbol local)
    {
        ArgumentNullException.ThrowIfNull(local, nameof(local));
        return _constantsBeingBound.Contains(local);
    }

    /* A run of statements with locals of its own. */
    internal BoundBlock BindBlock(Statement? syntax, StatementCollection statements)
    {
        ArgumentNullException.ThrowIfNull(statements, nameof(statements));

        BlockScope Scope = _body.PushBlock();
        DeclareLocals(Scope, statements);
        List<BoundStatement> Bound = new();
        foreach (Statement Statement in statements.Where(statement => statement is not EmptyStatement))
        {
            Bound.Add(BindStatement(Statement));
        }
        _body.PopScope(Scope);
        return new BoundBlock(syntax, Scope.Locals, Bound);
    }


    // Private methods.
    private BoundStatement BindStatement(Statement statement)
    {
        return statement switch
        {
            VariableDeclarationStatement Declaration => BindLocalDeclarations(Declaration),
            BlockStatement Block => BindBlock(Block, Block.Body),
            ReturnStatement Return when !_body.IsIterator => BindReturn(Return),
            ReturnStatement or IfStatement or WhileStatement or ForStatement or ForEachStatement or SwitchStatement
                or TryStatement or ThrowStatement or BreakStatement or ContinueStatement or YieldStatement
                => new BoundNotYetSupportedStatement(statement),
            _ => BindExpressionStatement(statement)
        };
    }

    /* The locals a block declares directly, each made and added to the block when it is entered, so that the
     * whole block finds them. A local whose name is taken is reported here, at its declaration: by another of the
     * block, by a local or a parameter around it, or by a generic parameter of the function. */
    private void DeclareLocals(BlockScope scope, StatementCollection statements)
    {
        ErrorRepository ErrorCreator = _body.Resolution.ErrorCreator;
        foreach (VariableDeclarationStatement Declaration in statements.OfType<VariableDeclarationStatement>())
        {
            foreach (VariableAssignment Declared in Declaration.Declarations)
            {
                string Name = Declared.SelfIdentifier.SourceCodeName;
                LocalSymbol Local = new(Name, Declaration.IsConstant, Declaration);
                _locals[Declared] = Local;
                if (!scope.TryAdd(Local))
                {
                    _body.AddError(ErrorCreator.DuplicateLocal.CreateOptions(Name), Declared.Origin);
                    continue;
                }

                string? OuterKind = FindOuterNameKind(scope.Parent, Name);
                if (OuterKind != null)
                {
                    _body.AddError(ErrorCreator.LocalHidesOuter.CreateOptions(Name, OuterKind), Declared.Origin);
                }
                else if (_body.FindFunctionGenericParameter(Name) != null)
                {
                    _body.AddError(ErrorCreator.LocalNamedLikeGenericParameter.CreateOptions(Name),
                        Declared.Origin);
                }
            }
        }
    }

    /* Whether a local or a parameter of a scope around has a name: "local", "parameter", or null for neither. */
    private string? FindOuterNameKind(LocalScope? scope, string name)
    {
        for (LocalScope? Scope = scope; Scope != null; Scope = Scope.Parent)
        {
            if ((Scope is BlockScope Block) && (Block.Find(name) != null))
            {
                return KGVL.NAME_LOCAL;
            }
            if ((Scope is ParameterScope Parameters) && Parameters.TryFind(name, out _, out _, out _))
            {
                return KGVL.NAME_PARAMETER;
            }
        }
        return null;
    }

    /* "Type a = 1, b;", "var a = 1;" or "const Type a = 1;". A local declared with "var" takes the type of its
     * starting value, which it has to have, and which has to have a type, so it can be declared only alone and
     * not as a constant. A constant's type is one a constant can have, and its value is known at compile time.
     * As in C#, a local of a written type can be named in its own starting value, being declared by then, which
     * one declared with "var" cannot. */
    private BoundStatement BindLocalDeclarations(VariableDeclarationStatement declaration)
    {
        ErrorRepository ErrorCreator = _body.Resolution.ErrorCreator;
        BlockScope Scope = (BlockScope)_body.Scope!;
        bool IsInferred = declaration.IsTypeInferred;
        bool IsValidInference = true;
        if (IsInferred && declaration.IsConstant)
        {
            _body.AddError(ErrorCreator.InferredConstant.CreateOptions(), declaration);
            IsValidInference = false;
        }
        else if (IsInferred && (declaration.Declarations.Count() > 1))
        {
            _body.AddError(ErrorCreator.InferredLocalsTogether.CreateOptions(), declaration);
            IsValidInference = false;
        }

        string FirstName = declaration.Declarations.First().SelfIdentifier.SourceCodeName;
        SemanticType? WrittenType = IsInferred ? null : _body.ReadType(declaration.Type!, declaration);
        if ((WrittenType != null) && (WrittenType is not ErrorType) && IsWrittenTypeWrong(declaration.Type!,
            FirstName, declaration))
        {
            WrittenType = ErrorType.Instance;
        }
        if ((WrittenType != null) && declaration.IsConstant && (WrittenType is not ErrorType)
            && !FieldTypeChecker.IsConstantType(WrittenType))
        {
            _body.AddError(ErrorCreator.ConstantLocalType.CreateOptions(FirstName, WrittenType.ToString()),
                declaration);
            WrittenType = ErrorType.Instance;
        }

        List<BoundLocalDeclaration> Declarations = new();
        foreach (VariableAssignment Declared in declaration.Declarations)
        {
            LocalSymbol Local = _locals[Declared];
            BoundExpression? Initializer = null;
            if (!IsInferred)
            {
                Local.Type = WrittenType!;
                Scope.MarkDeclared(Local);
                if (declaration.IsConstant)
                {
                    _constantsBeingBound.Add(Local);
                }
                Initializer = (Declared.Value == null) ? null
                    : _body.Expressions.BindConvertedValue(Declared.Value, Local.Type);
                _constantsBeingBound.Remove(Local);
            }
            else
            {
                Initializer = BindInferred(Declared, Local, IsValidInference, declaration);
                Scope.MarkDeclared(Local);
            }

            if (declaration.IsConstant && (Initializer != null) && (Local.Type is not ErrorType))
            {
                Local.ConstantValue = _body.GetConstantInitializerValue(Initializer, Local.Name, Declared.Value!);
            }
            Declarations.Add(new(declaration, Local, Initializer));
        }
        return new BoundLocalDeclarations(declaration, Declarations);
    }

    /* A local declared with "var", which takes the type of its starting value: one it has to be given, and which
     * has to have a type of its own, unlike the literal null or "default" alone. */
    private BoundExpression? BindInferred(VariableAssignment declared,
        LocalSymbol local,
        bool isValidInference,
        VariableDeclarationStatement declaration)
    {
        ErrorRepository ErrorCreator = _body.Resolution.ErrorCreator;
        if (declared.Value == null)
        {
            if (isValidInference)
            {
                _body.AddError(ErrorCreator.InferredLocalWithoutValue.CreateOptions(local.Name), declaration);
            }
            return null;
        }

        BoundExpression Value = _body.Expressions.BindValue(declared.Value);
        if ((Value.Type == null) && !Value.HasErrors)
        {
            _body.AddError((Value is BoundDefault) ? ErrorCreator.DefaultWithoutType.CreateOptions()
                : ErrorCreator.InferredLocalFromNull.CreateOptions(local.Name), declared.Value);
            return new BoundBadExpression(declared.Value, Value);
        }
        if ((Value.Type is DeclaredType Inferred) && MemberRelations.IsStaticClass(Inferred.Declaration))
        {
            _body.AddError(ErrorCreator.StaticClassAsType.CreateOptions(KGVL.NAME_LOCAL, local.Name,
                Inferred.ToString()), declared.Origin);
            return Value;
        }
        if (isValidInference && (Value.Type != null))
        {
            local.Type = Value.Type;
        }
        return Value;
    }

    /* Whether a local's written type is one C# rejects, which is reported: a static class, which has no
     * instances, as its whole type, or anything BodyBinder.CheckWrittenType reports in it. */
    private bool IsWrittenTypeWrong(TypeTargetIdentifier written, string localName, Statement where)
    {
        bool IsReported = false;
        if (StaticClassUsageChecker.IsStaticClass(written) && (written.ArrayRank == 0))
        {
            _body.AddError(_body.Resolution.ErrorCreator.StaticClassAsType.CreateOptions(KGVL.NAME_LOCAL,
                localName, written.ToString()), where);
            IsReported = true;
        }
        return _body.CheckWrittenType(written, KGVL.NAME_LOCAL, localName, where) || IsReported;
    }

    /* "return", with the value it gives converted to what the function returns, which it has to give exactly when
     * the function returns something. A value given where none can be is still bound, for its own mistakes. */
    private BoundStatement BindReturn(ReturnStatement statement)
    {
        ErrorRepository ErrorCreator = _body.Resolution.ErrorCreator;
        string Kind = MemberRelations.GetKindName(_body.Member);
        string Name = MemberRelations.GetDisplayName(_body.Member);
        if (statement.ReturnValue == null)
        {
            if ((_body.ReturnType != null) && (_body.ReturnType is not ErrorType))
            {
                _body.AddError(ErrorCreator.ReturnWithoutValue.CreateOptions(Kind, Name,
                    _body.ReturnType.ToString()), statement);
            }
            return new BoundReturn(statement, null);
        }
        if (_body.ReturnType == null)
        {
            /* As in C#, a value which is itself wrong has been reported, and is not reported again for being
             * given at all. */
            BoundExpression Unused = _body.Expressions.BindValue(statement.ReturnValue);
            if (!Unused.HasErrors)
            {
                _body.AddError(ErrorCreator.ReturnValueFromNothing.CreateOptions(Kind, Name), statement);
            }
            return new BoundReturn(statement, new BoundBadExpression(statement.ReturnValue, Unused));
        }
        return new BoundReturn(statement, _body.Expressions.BindConvertedValue(statement.ReturnValue,
            _body.ReturnType));
    }

    /* A value standing as a statement of its own, which, as in C#, only an assignment, a call, an increment or
     * decrement, or the creation of an object can, since working out any other value does nothing with it. */
    private BoundStatement BindExpressionStatement(Statement syntax)
    {
        BoundExpression Value = _body.Expressions.BindValue(syntax);
        if (!Value.HasErrors && !IsStatementValue(syntax))
        {
            _body.AddError(_body.Resolution.ErrorCreator.ValueAsStatement.CreateOptions(), syntax);
        }
        return new BoundExpressionStatement(syntax, Value);
    }

    /* Whether a value can stand as a statement: an assignment, a call, which may end a chain or follow a "?.",
     * an increment or decrement, or the creation of an object, and not written in brackets. */
    private bool IsStatementValue(Statement syntax)
    {
        if (syntax.IsParenthesized)
        {
            return false;
        }
        return syntax switch
        {
            AssignmentStatement or FunctionCallStatement => true,
            CompositeAccessStatement Chain => Chain.Components.LastOrDefault() is FunctionCallStatement,
            UnaryOperatorStatement Unary => Unary.Operator is StatementOperator.Increment
                or StatementOperator.Decrement,
            BinaryOperatorStatement { Operator: StatementOperator.ContinueIfNotNull } Conditional
                => IsStatementValue(Conditional.Right) && (Conditional.Right is not AssignmentStatement),
            _ => false
        };
    }
}
