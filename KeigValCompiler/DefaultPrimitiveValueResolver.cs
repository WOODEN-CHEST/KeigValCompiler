using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Member;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler;

internal class DefaultPrimitiveValueResolver : IPrimitiveValueResolver
{
    // Private methods.
    private PackMember? GetTypeOfNumber(GenericNumber number, BuiltInTypeRegistry registry)
    {
        if (TwoIntDecimal.TryParse(number.Number, out _))
        {
            return registry.TypeDecimal;
        }
        if (number.IsLong)
        {
            return number.IsUnsigned ? registry.TypeUInt64 : registry.TypeInt64;
        }
        return number.IsUnsigned ? registry.TypeUInt32 : registry.TypeInt32;
    }


    // Inherited methods.
    public PackMember? GetTypeOfValue(object value, DataPack pack, BuiltInTypeRegistry registry)
    {
        if (value is GenericNumber Number)
        {
            return GetTypeOfNumber(Number, registry);
        }

        if (value is not string StringValue)
        {
            return null;
        }

        if (StringValue.StartsWith(KGVL.SINGLE_QUOTE) && StringValue.EndsWith(KGVL.SINGLE_QUOTE))
        {
            return registry.TypeChar;
        }
        if (StringValue.StartsWith(KGVL.DOUBLE_QUOTE) && StringValue.EndsWith(KGVL.DOUBLE_QUOTE))
        {
            return registry.TypeString;
        }
        if (StringValue is KGVL.KEYWORD_TRUE or KGVL.KEYWORD_FALSE)
        { 
            return registry.TypeBool;
        }
        if (StringValue == KGVL.KEYWORD_NULL)
        {
            return registry.TypeNull;
        }
        return null;
    }


    // Types.
    private record class NumberMetaInfo(NumberBase Base, long Value, PackMember NumberType);
}