﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal interface IGenericParameterHolder
{
    // Fields.
    GenericTypeParameterCollection GenericParameters { get; }
}