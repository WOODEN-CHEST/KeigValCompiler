using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

internal class CompilerMessageCategory
{
    // Static fields.
    public static CompilerMessageCategory SourceFileRoot { get; } = new("Source File Root", "SFR");
    public static CompilerMessageCategory MemberModifier { get; } = new("Member Modifier", "MM");
    public static CompilerMessageCategory MemberGeneric { get; } = new("Member Generic", "MG");
    public static CompilerMessageCategory TypeMemberCommon { get; } = new("Type Member Common", "TMC");
    public static CompilerMessageCategory Delegate { get; } = new("Delegate", "DE");
    public static CompilerMessageCategory Event { get; } = new("Event", "EV");
    public static CompilerMessageCategory Enum { get; } = new("Enum", "EN");
    public static CompilerMessageCategory ReturnTypedMembersCommon { get; } 
        = new("Return Typed Member Common", "RTMC");
    public static CompilerMessageCategory Identifiers { get; } = new("Identifiers", "ID");
    public static CompilerMessageCategory Record { get; } = new("Record", "RE");
    public static CompilerMessageCategory Generics { get; } = new("Generics", "GE");
    public static CompilerMessageCategory Comment { get; } = new("Comment", "CO");



    // Fields.
    internal string Name { get; private init; }
    internal string Prefix { get; private init; }


    // Constructors.
    private CompilerMessageCategory(string name, string prefix)
    {
        Name = name;
        Prefix = prefix;
    }
}