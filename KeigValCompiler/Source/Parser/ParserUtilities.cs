using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

internal class ParserUtilities
{
    // Methods.
    internal string MemberHolderToString(object holder)
    {
        return holder switch
        {
            PackNameSpace => KGVL.NAME_NAMESPACE,
            PackClass ClassHolder => ClassHolder.HasModifier(PackMemberModifiers.Record)
                ? KGVL.NAME_RECORD_CLASS : KGVL.NAME_CLASS,
            PackStruct => KGVL.NAME_STRUCT,
            PackInterface => KGVL.NAME_INTERFACE,
            PackEnumeration => KGVL.NAME_ENUM,
            PackDelegate => KGVL.NAME_DELEGATE,
            PackField => KGVL.NAME_FIELD,
            PackProperty => KGVL.NAME_PROPERTY,
            PackIndexer => KGVL.NAME_INDEXER,
            PackEvent => KGVL.NAME_EVENT,
            _ => "(internal error evaluating member type, invalid type)"
        };
    }

    internal TypeTargetIdentifier ParseTypeTargetIdentifier(SourceDataParser parser, ErrorCreateOptions? error)
    {
        string BaseName = parser.ReadIdentifier(error);
        parser.SkipUntilNonWhitespace(null);
        if (parser.GetCharAtDataIndex() != KGVL.GENERIC_TYPE_START)
        {
            return new TypeTargetIdentifier(new(BaseName), null);
        }

        parser.IncrementDataIndex();
        List<TypeTargetIdentifier> SubTypes = new();
        bool IsTypeNameExpected = true;

        while (IsTypeNameExpected)
        {
            parser.SkipUntilNonWhitespace(error);
            TypeTargetIdentifier SubType = ParseTypeTargetIdentifier(parser, error);
            SubTypes.Add(SubType);
            parser.SkipUntilNonWhitespace(error);
            IsTypeNameExpected = parser.GetCharAtDataIndex() == KGVL.COMMA;
            if (IsTypeNameExpected)
            {
                parser.IncrementDataIndex();
            }
        }

        if (parser.GetCharAtDataIndex() != KGVL.GENERIC_TYPE_END)
        {
            throw new SourceFileReadException(parser, error, 
                $"Expected generic type end '{KGVL.GENERIC_TYPE_END}'");
        }
        parser.IncrementDataIndex();

        return new(new(BaseName), SubTypes.ToArray());
    }


    // Private methods.

}