using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Function;

internal abstract class Statement
{
    // Methods.
    public abstract IIdentifiable? GetReturnType();
    public abstract object? GetValue();
}