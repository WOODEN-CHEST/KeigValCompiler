using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class MinecraftStringIDProvider : IStringIDProvider
{
    // Private fields.
    private ulong _availableID = 1uL;
    private readonly char[] Characters =
    {
        '0','1','2','3','4','5','6','7','8','9',
        'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
        'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
        '_', '-', '.'
    };


    // Inherited methods.
    public string Get(ulong id)
    {
        StringBuilder Builder = new(8);

        for (ulong Value = id; Value > 0; Value /= (ulong)Characters.Length)
        {
            Builder.Insert(0, Characters[(int)(Value % (ulong)Characters.Length)]);
        }

        return Builder.ToString();
    }

    public string GetNext()
    {
        string Value = Get(_availableID);
        _availableID++;
        return Value;
    }
}