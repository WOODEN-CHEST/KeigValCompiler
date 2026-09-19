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

    /* Unary. */
    Negation,
    UnaryPlus,
    Not,
    BitwiseComplement,

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

    NotNullOrElse,
    ContinueIfNotNull,

    EventSubscribe,
    EventUnsubscribe,

    IsCheck
}