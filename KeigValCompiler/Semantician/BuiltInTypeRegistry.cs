using KeigValCompiler.Semantician.Library;
using KeigValCompiler.Semantician.Member;

namespace KeigValCompiler.Semantician;

/* Which type the standard library declares for each type the compiler knows by name, and the way
 * back. It is filled once the library's types are collected, and is how "int" finds KGVL.Int32. */
internal class BuiltInTypeRegistry
{
    // Private fields.
    private readonly Dictionary<LibraryType, PackMember> _typesByLibraryType = new();

    /* Keyed by the object rather than by PackMember's own equality, which compares resolved names. */
    private readonly Dictionary<PackMember, LibraryType> _libraryTypesByType =
        new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, LibraryType> _libraryTypesByKeyword = new();


    // Internal methods.
    internal void AddType(LibraryType libraryType, PackMember type)
    {
        ArgumentNullException.ThrowIfNull(libraryType, nameof(libraryType));
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        _typesByLibraryType[libraryType] = type;
        _libraryTypesByType[type] = libraryType;
        if (libraryType.Keyword != null)
        {
            _libraryTypesByKeyword[libraryType.Keyword] = libraryType;
        }
    }

    internal PackMember? GetDeclaredType(LibraryType libraryType)
    {
        ArgumentNullException.ThrowIfNull(libraryType, nameof(libraryType));

        _typesByLibraryType.TryGetValue(libraryType, out PackMember? DeclaredType);
        return DeclaredType;
    }

    /* The type a keyword such as "int" stands for, or null if the library declares none for it. */
    internal PackMember? GetTypeByKeyword(string keyword)
    {
        ArgumentNullException.ThrowIfNull(keyword, nameof(keyword));

        return _libraryTypesByKeyword.TryGetValue(keyword, out LibraryType? KeywordType)
            ? GetDeclaredType(KeywordType) : null;
    }

    /* Which known type a declared type is, or null for one the compiler does not know by name. */
    internal LibraryType? GetLibraryType(PackMember type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        _libraryTypesByType.TryGetValue(type, out LibraryType? KnownType);
        return KnownType;
    }
}
