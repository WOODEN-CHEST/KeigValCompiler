using KeigValCompiler.Error;
using KeigValCompiler.Semantician;
using KeigValCompiler.Semantician.Member;
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
            int PreWordIndex = Parser.DataIndex;
            TypeTargetIdentifier Word = Parser.ReadTypeTargetIdentifier(GetRootKeywordError());
            string ExtractedKeyword = Word.MainTarget!.SourceCodeName;

            if (ExtractedKeyword == KGVL.KEYWORD_NAMESPACE)
            {
                _activeNamespace = GetOrCreateNamespace(ParseNamespaceName(false), false);
                continue;
            }
            else if (ExtractedKeyword == KGVL.KEYWORD_USING)
            {
                ParseUsingStatement();
                continue;
            }
            else if (_activeNamespace == null)
            {
                throw new SourceFileReadException(Parser,
                    ErrorCreator.RootNonActiveNamespace.CreateOptions(ExtractedKeyword));
            }

            Parser.DataIndex = PreWordIndex;
            _memberParser.ParseMember(_activeNamespace, KGVL.NAME_NAMESPACE,
                _activeNamespace.SelfIdentifier.SourceCodeName);
        }
    }


    // Private methods.
    private ErrorCreateOptions GetRootKeywordError()
    {
        if (_activeNamespace == null)
        {
            return ErrorCreator.RootExpectedKeyword.CreateOptions();
        }
        return ErrorCreator.RootExpectedKeywordOrMember.CreateOptions(_activeNamespace.SelfIdentifier.SourceCodeName);
    }

    private ErrorCreateOptions GetExpectedNamespaceError(bool isUsingDirective)
    {
        if (isUsingDirective)
        {
            return ErrorCreator.ExpectedNamespaceForUsingDirective.CreateOptions();
        }
        return ErrorCreator.ExpectedNamespaceForSet.CreateOptions();
    }

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

    private string ParseNamespaceName(bool isUsingDirective)
    {
        Parser.SkipUntilNonWhitespace(GetExpectedNamespaceError(isUsingDirective));

        StringBuilder NamespaceBuilder = new();
        bool IsMoreNamespaceExpected = true;

        while ((Parser.GetCharAtDataIndex() != KGVL.SEMICOLON) || IsMoreNamespaceExpected)
        {
            NamespaceBuilder.Append(Parser.ReadIdentifier(ErrorCreator.ExpectedNamespaceSectionIdentifier
                .CreateOptions(NamespaceBuilder.ToString())));

            string IncompleteNamespaceName = NamespaceBuilder.ToString();
            Parser.SkipUntilNonWhitespace(ErrorCreator.ExpectedNamespaceEndOrContinuation
                .CreateOptions(IncompleteNamespaceName));

            char CharAfterIdentifier = Parser.GetCharAtDataIndex();
            if (CharAfterIdentifier == KGVL.NAMESPACE_SEPARATOR)
            {
                NamespaceBuilder.Append(KGVL.NAMESPACE_SEPARATOR);
                Parser.IncrementDataIndex();
                IsMoreNamespaceExpected = true;
                Parser.SkipUntilNonWhitespace(ErrorCreator.NamespaceEOFTrailingContinuation
                    .CreateOptions(IncompleteNamespaceName));
            }
            else if (CharAfterIdentifier == KGVL.SEMICOLON)
            {
                IsMoreNamespaceExpected = false;
            }
            else
            {
                throw new SourceFileReadException(Parser, ErrorCreator.NamespaceUnexpectedChar
                    .CreateOptions(Parser.GetCharAtDataIndex(), IncompleteNamespaceName));
            }
        }

        Parser.IncrementDataIndex();
        return NamespaceBuilder.ToString();
    }

    private void ParseUsingStatement()
    {
        SourceFile.AddNamespaceImport(GetOrCreateNamespace(ParseNamespaceName(true), true));
    }
}