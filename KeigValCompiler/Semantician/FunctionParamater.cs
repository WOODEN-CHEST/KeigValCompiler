﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class FunctionParamater
{
    // Internal fields.
    internal string Name { get; private init; }
    internal string Type { get; private init; }
    internal FunctionParameterModifier Modifiers { get; private init; }


    // Constructors.
    internal FunctionParamater(string type, string name, FunctionParameterModifier modifier)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Modifiers = modifier;
    }
}