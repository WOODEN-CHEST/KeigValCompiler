﻿using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler;

internal interface IIdentifierSearcher
{
    PackMember? GetTypeFromCodeName(string codeName, PackSourceFile sourceFile, BuiltInTypeRegistry registry);
}