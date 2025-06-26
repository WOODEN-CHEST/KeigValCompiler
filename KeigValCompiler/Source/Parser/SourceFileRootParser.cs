using KeigValCompiler.Error;
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
    public SourceFileRootParser(PackParsingContext context) : base(context)
    {
        _memberParser = new(context);
    }


    // Methods.
    internal void ParseBase()
    {
        while (Parser.SkipUntilNonWhitespace(null))
        {
            string Keyword = Parser.ReadWord(ErrorCreator.RootExpectedKeyword.CreateOptions());

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
                throw new SourceFileReadException(Parser,
                    ErrorCreator.RootNonActiveNamespace.CreateOptions(Keyword));
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
        Parser.SkipUntilNonWhitespace(ErrorCreator.RootInvalidNamespace.CreateOptions());

        StringBuilder NamespaceBuilder = new();
        bool IsMoreNamespaceExpected = true;

        while ((Parser.GetCharAtDataIndex() != KGVL.SEMICOLON) || IsMoreNamespaceExpected)
        {
            NamespaceBuilder.Append(Parser.ReadIdentifier(ErrorCreator.RootInvalidNamespace.CreateOptions()));
            Parser.SkipUntilNonWhitespace(ErrorCreator.NamespaceEndOrContinuation
                .CreateOptions(NamespaceBuilder.ToString()));

            char CharAfterIdentifier = Parser.GetCharAtDataIndex();
            if (CharAfterIdentifier == KGVL.NAMESPACE_SEPARATOR)
            {
                NamespaceBuilder.Append(KGVL.NAMESPACE_SEPARATOR);
                Parser.IncrementDataIndex();
                IsMoreNamespaceExpected = true;
                Parser.SkipUntilNonWhitespace(ErrorCreator.RootInvalidNamespace.CreateOptions());
            }
            else if (CharAfterIdentifier == KGVL.SEMICOLON)
            {
                IsMoreNamespaceExpected = false;
            }
            else
            {
                throw new SourceFileReadException(Parser, ErrorCreator.NamespaceEndOrContinuation
                    .CreateOptions(NamespaceBuilder.ToString()));
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