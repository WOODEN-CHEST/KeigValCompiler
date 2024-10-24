using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal interface IPackFunctionHolder
{
    // Fields.
    IEnumerable<PackFunction> Functions { get; }
    IEnumerable<PackFunction> AllFunctions { get; }
    IEnumerable<PackProperty> Properties { get; }
    IEnumerable<PackProperty> AllProperties { get; }
    IEnumerable<PackIndexer> Indexers { get; }
    IEnumerable<PackIndexer> AllIndexers { get; }


    // Methods.
    void AddFunction(PackFunction function);
    void AddProperty(PackProperty function);
    void AddIndexer(PackIndexer function);
    void RemoveFunction(PackFunction function);
    void RemoveProperty(PackProperty function);
    void RemoveIndexer(PackIndexer function);
}