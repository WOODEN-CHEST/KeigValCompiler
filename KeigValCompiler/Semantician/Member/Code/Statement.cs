﻿namespace KeigValCompiler.Semantician.Member.Code;

internal abstract class Statement
{
    // Fields.
    internal virtual Identifier? StatementReturnType { get; set; }
}