using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal interface IPackEventHolder
{
    // Fields.
    IEnumerable<PackEvent> Events { get; }
    IEnumerable<PackEvent> AllEvents { get; }


    // Methods.
    void AddEvent(PackEvent packEvent);
    void RemoveEvent(PackEvent packEvent);
}