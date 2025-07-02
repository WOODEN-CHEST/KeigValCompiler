using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler;

internal record IntegerNumber(string Number, NumberBase Base, bool IsLong, bool IsUnsigned);