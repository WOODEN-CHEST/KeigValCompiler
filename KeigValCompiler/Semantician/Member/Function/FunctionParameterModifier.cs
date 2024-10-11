﻿namespace KeigValCompiler.Semantician.Member.Function;

[Flags]
internal enum FunctionParameterModifier
{
    None = 0,
    In = 1,
    Out = 2,
    Ref = 3
}