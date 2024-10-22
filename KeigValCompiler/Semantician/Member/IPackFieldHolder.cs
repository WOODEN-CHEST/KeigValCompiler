using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal interface IPackFieldHolder
{
    // Fields.
    PackField[] Fields { get; }


    // Methods.
    void AddField(PackField field);
    void RemoveField(PackField field);
}