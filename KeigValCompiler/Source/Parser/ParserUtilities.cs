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
            PackNameSpace => "namespace",
            PackClass ClassHolder => ClassHolder.HasModifier(PackMemberModifiers.Record) ? "record class" : "class",
            PackStruct => "struct",
            PackInterface => "interface",
            PackEnumeration => "enum",
            PackDelegate => "delegate",
            PackField => "field",
            PackProperty => "property",
            PackIndexer => "indexer",
            PackEvent => "event",
            _ => "(internal error evaluating member type, invalid type)"
        };
    }
}