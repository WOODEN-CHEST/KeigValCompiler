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
    private NumberMetaInfo? ReadInteger(string number, BuiltInTypeRegistry registry)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return null;
        }

        NumberBase Base;
        int StartIndex = 0;
        if (number.StartsWith(KGVL.PREFIX_BINARY))
        {
            Base = NumberBase.Base2;
            StartIndex = KGVL.PREFIX_BINARY.Length;
        }
        else if (number.StartsWith(KGVL.PREFIX_HEX))
        {
            Base = NumberBase.Base16;
            StartIndex = KGVL.PREFIX_HEX.Length;
        }
        else
        {
            Base = NumberBase.Base10;
        }

        PackMember TargetType = GetTypeOfNumber(number, registry, out int EndIndex);
        string CroppedNumber = number[StartIndex..EndIndex];
        if ((TargetType == registry.TypeUInt64) && ulong.TryParse(CroppedNumber, out ulong ULongValue))
        {
            return new(Base, (long)ULongValue, TargetType);
        }
        else if (long.TryParse(CroppedNumber, out long LongValue))
        {
            return new(Base, LongValue, TargetType);
        }
        return null;
    }

    private PackMember GetTypeOfNumber(string number, BuiltInTypeRegistry registry, out int endIndex)
    {
        bool IsLong = false;
        bool IsUnsigned = false;
        endIndex = 0;

        for (int i = number.Length - 1; i >= 0; i--)
        {
            if (!IsUnsigned && (number[i] == KGVL.SUFFIX_UNSIGNED))
            {
                IsUnsigned = true;
            }
            else if (!IsLong && (number[i] == KGVL.SUFFIX_LONG))
            {
                IsLong = true;
            }
            else
            {
                endIndex = i + 1;
                break;
            }
        }

        if (IsLong && IsUnsigned)
        {
            return registry.TypeUInt64;
        }
        else if (IsLong && !IsUnsigned)
        {
            return registry.TypeInt64;
        }
        else if (!IsLong && IsUnsigned)
        {
            return registry.TypeUInt32;
        }
        else
        {
            return registry.TypeInt32;
        }
    }



    // Inherited methods.
    public PackMember? GetTypeOfValue(string value, DataPack pack, BuiltInTypeRegistry registry)
    {
        NumberMetaInfo? NumberInfo = ReadInteger(value, registry);
        if (NumberInfo != null)
        {
            return NumberInfo.NumberType;
        }
        if (TwoIntDecimal.TryParse(value, out var _))
        {
            return registry.TypeDecimal;
        }
        if (value.StartsWith(KGVL.SINGLE_QUOTE) && value.EndsWith(KGVL.SINGLE_QUOTE))
        {
            return registry.TypeChar;
        }
        if (value.StartsWith(KGVL.DOUBLE_QUOTE) && value.EndsWith(KGVL.DOUBLE_QUOTE))
        {
            return registry.TypeString;
        }
        if (value is KGVL.KEYWORD_TRUE or KGVL.KEYWORD_FALSE)
        { 
            return registry.TypeBool;
        }
        if (value == KGVL.KEYWORD_NULL)
        {
            return registry.TypeNull;
        }
        return null;
    }


    // Types.
    private enum NumberBase
    {
        Base10,
        Base2,
        Base16
    }

    private record class NumberMetaInfo(NumberBase Base, long Value, PackMember NumberType);
}