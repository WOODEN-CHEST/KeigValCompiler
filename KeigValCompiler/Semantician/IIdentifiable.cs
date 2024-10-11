using KeigValCompiler.Semantician.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal interface IIdentifiable
{
    Identifier ItemIdentifier { get; }
}