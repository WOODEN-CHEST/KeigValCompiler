using KeigValCompiler.Semantician.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class BuiltInTypeRegistry
{
    // InternalfFields.
    internal PackStruct TypeInt8 { get; set; }
    internal PackStruct TypeUInt8 { get; set; }
    internal PackStruct TypeInt16 { get; set; }
    internal PackStruct TypeUInt16 { get; set; }
    internal PackStruct TypeInt32 { get; set; }
    internal PackStruct TypeUInt32 { get; set; }
    internal PackStruct TypeInt64 { get; set; }
    internal PackStruct TypeUInt64 { get; set; }
    internal PackStruct TypeDecimal { get; set; }
    internal PackStruct TypeBool { get; set; }
    internal PackStruct TypeChar { get; set; }
    internal PackClass TypeString { get; set; }
    internal PackStruct TypeNull { get; set; }


    // Private fields.
    private readonly Dictionary<string, PackMember> _shorthandTypeMap = new();


    // Methods.
    public void AddShorthandType(string shorthandName, PackMember type)
    {
        ArgumentNullException.ThrowIfNull(shorthandName, nameof(shorthandName));
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        _shorthandTypeMap[shorthandName] = type;
    }

    public PackMember? GetTypeFromShorthandName(string shorthandName)
    {
        _shorthandTypeMap.TryGetValue(shorthandName, out PackMember? TargetType);
        return TargetType;
    }
}