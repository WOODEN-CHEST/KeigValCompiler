using KeigValCompiler.Error;
using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;
using KeigValCompiler.Semantician.Types;

namespace KeigValCompiler.Semantician.Resolver;

internal class PackResolutionContext
{
    // Fields.
    internal required DataPack Pack { get; init; }
    internal required BuiltInTypeRegistry Registry { get; init; }
    internal required TypeSearcher TypeSearcher { get; init; }

    /* Only usable once types are resolved and the library's known types found. */
    internal required SemanticTypeReader TypeReader { get; init; }
    internal required LibraryBindingTable BindingTable { get; init; }
    internal required IdentifierGenerator IdentifierGenerator { get; init; }
    internal required ErrorRepository ErrorCreator { get; init; }
    internal required CompilerMessageCollection Messages { get; init; }


    // Internal methods.
    /* Resolution errors are queued like the parser's, so that one missing type does not hide the rest.
     * Declarations carry their line but not their column. */
    internal void AddError(ErrorCreateOptions error, PackMember member, string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        Messages.AddError(error, GetLocation(member), notes);
    }

    /* For what is written at a line of its own but is not a member, such as an enum constant or a
     * "using" directive. */
    internal void AddError(ErrorCreateOptions error, PackSourceFile file, SourceFileOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(file, nameof(file));
        Messages.AddError(error, new(file.Path, origin.Line, CompilerMessageLocation.COLUMN_UNKNOWN), null);
    }

    internal void AddWarning(WarningCreateOptions warning, PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        Messages.AddWarning(warning, GetLocation(member), null);
    }

    internal CompilerMessageLocation GetLocation(PackMember member)
    {
        ArgumentNullException.ThrowIfNull(member, nameof(member));
        return new(member.SourceFile.Path, member.SourceFileOrigin.Line, CompilerMessageLocation.COLUMN_UNKNOWN);
    }

    /* Whether any error so far is in one of the standard library's files. */
    internal bool HasLibraryErrors()
    {
        HashSet<string> LibraryPaths = Pack.SourceFiles.Where(file => file.IsLibraryFile)
            .Select(file => file.Path).ToHashSet();
        return Messages.Any(message => message.IsError && (message.Location.FilePath != null)
            && LibraryPaths.Contains(message.Location.FilePath));
    }
}
