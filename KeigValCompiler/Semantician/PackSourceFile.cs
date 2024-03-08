using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackSourceFile
{
    // Internal fields.
    internal DataPack Pack { get; private set; }


    // Constructors.
    internal PackSourceFile(DataPack pack)
    {
        Pack = pack ?? throw new ArgumentNullException(nameof(pack));
    }
}