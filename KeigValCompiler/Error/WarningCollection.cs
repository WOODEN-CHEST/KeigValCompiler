using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

internal class WarningCollection : IEnumerable<WarningCreateOptions>
{
    // Private fields.
    private readonly List<WarningCreateOptions> _warnings = new();


    // Methods.
    public void Add(WarningCreateOptions warning)
    {
        _warnings.Add(warning);
    }


    // Inherited methods.
    public IEnumerator<WarningCreateOptions> GetEnumerator()
    {
        return _warnings.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}