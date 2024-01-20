using KeigValCompiler.Source.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace KeigValCompiler.Source;

internal class NameSpace
{
    // Fields.
    internal string Name { get; private set; }


    // Private fields.
    private bool _isImplicitNamespace = false;


    // Constructors.
    public NameSpace() { }



    // Internal static methods.
    internal NameSpace Parse(SourceFileParser sourceParser)
    {
        NameSpace NameSpace = new();
        ParseName(sourceParser, NameSpace)


        throw new NotImplementedException();
    }


    // Private static methods.
    private static void ParseName(SourceFileParser sourceParser, NameSpace nameSpace)
    {
        char Char = '\0';
        StringBuilder ParsedName = new(32);
        
        while ((Char = sourceParser.ReadChar("Expected namespace end as ';' or '{'")) is not '{' or ';')
        {
            ParsedName.Append(Char);
        }

        if (ParsedName.Length == 0)
        {
            throw new SourceFileException(sourceParser, "Namespace's name is empty!");
        }

        nameSpace.Name = ParsedName.ToString();
        nameSpace._isImplicitNamespace = Char == ';';
    }
}