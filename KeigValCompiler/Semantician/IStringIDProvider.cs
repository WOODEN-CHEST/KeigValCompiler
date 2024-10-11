﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal interface IStringIDProvider
{
    string GetNext();
    string Get(ulong id);
}