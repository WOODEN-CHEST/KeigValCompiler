using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Generics;

internal class GenericTypeParameterCollection : IEnumerable<GenericTypeParameter>
{
    // Private fields.
    private readonly GenericTypeParameter[] _parameters;


    // Constructors.
    internal GenericTypeParameterCollection(GenericTypeParameter[] parameters)
    {
        _parameters = parameters?.ToArray() ?? throw new ArgumentNullException(nameof(parameters));
    }


    // Methods.
    public IEnumerator<GenericTypeParameter> GetEnumerator()
    {
        foreach (GenericTypeParameter Parameter in _parameters)
        {
            yield return Parameter;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}