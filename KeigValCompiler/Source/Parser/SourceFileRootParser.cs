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
    // Static fields.
    /* Everything at the root of a file ends in a semicolon, and there is no enclosing construct whose
     * closing bracket would mean anything here, so a stray one is just skipped past. */
    private static readonly char[] _rootResumeChars = new char[] { KGVL.SEMICOLON };
    private static readonly char[] _rootTerminatorChars = Array.Empty<char>();


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
            try
            {
                ParseRootEntry();
            }
            catch (SourceFileReadException e)
            {
                if (!RecoverFromError(e, _rootResumeChars, _rootTerminatorChars))
                {
                    return;
                }
            }
        }
    }


    // Private methods.
    private void ParseRootEntry()
    {
        int PreWordIndex = Parser.DataIndex;
        TypeTargetIdentifier Word = Parser.ReadTypeTargetIdentifier(GetRootKeywordError());
        string ExtractedKeyword = Word.MainTarget.SourceCodeName;

        if (ExtractedKeyword == KGVL.KEYWORD_NAMESPACE)
        {
            _activeNamespace = GetOrCreateNamespace(ParseNamespaceName(false), false);
            return;
        }
        else if (ExtractedKeyword == KGVL.KEYWORD_USING)
        {
            ParseUsingStatement();
            return;
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
        CompilerMessageLocation DirectiveLocation = GetCurrentLocation();
        string NamespaceName = ParseNamespaceName(true);

        if (IsNamespaceAlreadyImported(NamespaceName))
        {
            AddWarning(ErrorCreator.DuplicateUsingDirective.CreateOptions(NamespaceName), DirectiveLocation);
            return;
        }

        GetOrCreateNamespace(NamespaceName, true);
    }

    /* Compared by name rather than by instance, because a namespace which no file declares is built
     * fresh on every import of it and so would never match itself by reference. */
    private bool IsNamespaceAlreadyImported(string fullName)
    {
        foreach (PackNameSpace ImportedNameSpace in SourceFile.NamespaceImports)
        {
            if (ImportedNameSpace.SelfIdentifier.SourceCodeName == fullName)
            {
                return true;
            }
        }
        return false;
    }
}