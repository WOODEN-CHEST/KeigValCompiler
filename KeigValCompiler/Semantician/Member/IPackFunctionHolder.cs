using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal interface IPackFunctionHolder
{
    // Fields.
    PackFunction[] Functions { get; }
    PackProperty[] Properties { get; }
    PackIndexer[] Indexers { get; }


    // Methods.
    void AddFunction(PackFunction function);
    void AddProperty(PackProperty function);
    void AddIndexer(PackIndexer function);
    void RemoveFunction(PackFunction function);
    void RemoveProperty(PackProperty function);
    void RemoveIndexer(PackIndexer function);
}