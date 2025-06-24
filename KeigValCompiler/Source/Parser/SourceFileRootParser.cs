using KeigValCompiler.Semantician;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source.Parser;

internal class SourceFileRootParser : AbstractParserBase
{
    // Private fields.
    private PackNameSpace? _activeNamespace;

    private readonly MemberParser _memberParser;


    // Constructors.
    public SourceFileRootParser(SourceDataParser parser, ParserUtilities utils, PackSourceFile sourceFile)
        : base(parser, utils, sourceFile)
    {
        _memberParser = new(parser, utils, sourceFile);
    }


    // Methods.
    internal void ParseBase()
    {
        while (Parser.SkipUntilNonWhitespace(null))
        {
            string Keyword = Parser.ReadWord(
                $"Expected keyword '{KGVL.KEYWORD_NAMESPACE}' or '{KGVL.KEYWORD_USING}'.");

            if (Keyword == KGVL.KEYWORD_NAMESPACE)
            {
                _activeNamespace = GetOrCreateNamespace(ParseNamespaceName(), false);
                continue;
            }
            else if (Keyword == KGVL.KEYWORD_USING)
            {
                ParseUsingStatement();
                continue;
            }
            else if (_activeNamespace == null)
            {
                throw new SourceFileReadException(Parser.FilePath,
                    Parser.Line, "Expected namespace or using statement.");
            }

            Parser.ReverseUntilOneAfterWhitespace();
            _memberParser.ParseMember(_activeNamespace);
        }
    }


    // Private methods.
    private PackNameSpace GetOrCreateNamespace(string fullName, bool isImport)
    {
        PackNameSpace? NameSpace = SourceFile.Pack.TryGetNamespace(fullName);
        if (NameSpace != null)
        {
            return NameSpace;
        }

        NameSpace = new(new(fullName));

        if (isImport)
        {
            SourceFile.AddNamespaceImport(NameSpace);
        }
        else
        {
            SourceFile.AddNamespace(NameSpace);
        }

        return NameSpace;
    }

    private string ParseNamespaceName()
    {
        const string EXCEPTION_MSG_NAMESPACE_NAME = "Expected namespace name.";
        Parser.SkipUntilNonWhitespace(EXCEPTION_MSG_NAMESPACE_NAME);

        StringBuilder NamespaceBuilder = new();
        bool IsMoreNamespaceExpected = true;

        while ((Parser.GetCharAtDataIndex() != KGVL.SEMICOLON) || IsMoreNamespaceExpected)
        {
            NamespaceBuilder.Append(Parser.ReadIdentifier(EXCEPTION_MSG_NAMESPACE_NAME));
            Parser.SkipUntilNonWhitespace($"Expected '{KGVL.NAMESPACE_SEPARATOR}' or '{KGVL.SEMICOLON}' " +
                "(Namespace continuation or end)");

            char CharAfterIdentifier = Parser.GetCharAtDataIndex();
            if (CharAfterIdentifier == KGVL.NAMESPACE_SEPARATOR)
            {
                NamespaceBuilder.Append(KGVL.NAMESPACE_SEPARATOR);
                Parser.IncrementDataIndex();
                IsMoreNamespaceExpected = true;
                Parser.SkipUntilNonWhitespace(EXCEPTION_MSG_NAMESPACE_NAME);
            }
            else if (CharAfterIdentifier == KGVL.SEMICOLON)
            {
                IsMoreNamespaceExpected = false;
            }
            else
            {
                throw new SourceFileReadException($"Expected '{KGVL.SEMICOLON}' " +
                    $"after namespace end, got '{CharAfterIdentifier}'");
            }
        }

        Parser.IncrementDataIndex();
        return NamespaceBuilder.ToString();
    }

    private void ParseUsingStatement()
    {
        SourceFile.AddNamespaceImport(GetOrCreateNamespace(ParseNamespaceName(), true));
    }
}