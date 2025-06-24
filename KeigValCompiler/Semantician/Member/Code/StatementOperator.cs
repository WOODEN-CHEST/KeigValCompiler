using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

public enum StatementOperator
{
    Addition,
    Subtraction,
    Multiplication,
    Division,
    Modulo,

    Negation,

    Increment,
    Decrement,

    Equals,
    NotEquals,
    LargerThan,
    LessThan,
    LargerOrEqual,
    LessThanOrEqual,
    ConditionalAnd,
    ConditionalOr,

    LeftShift,
    RightShift,
    UnsignedRightShift,

    BitwiseAnd,
    BitwiseOr,
    BitwiseXor,

    NullSelection,
    ContinueIfNotNull,

    EventSubscribe,
    EventUnsubscribe,

    IsCheck
}