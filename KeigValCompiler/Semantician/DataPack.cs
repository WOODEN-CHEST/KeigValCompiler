using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace KeigValCompiler.Semantician;

internal class DataPack
{
    // Internal fields.
    internal PackClass[] Classes => _classes.ToArray();



    // Private fields.
    private readonly HashSet<PackClass> _classes = new();


    // Constructors.
    internal DataPack()
    {
        
    }


    // Internal methods.
    internal void AddClass(PackClass packClass)
    {
        _classes.Add(packClass ?? throw new ArgumentNullException(nameof(packClass)));
    }
}