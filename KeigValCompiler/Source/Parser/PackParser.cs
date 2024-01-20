using KeigValCompiler.Middle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Source;

internal class PackParser
{
    // Internal fields.
    internal DataPack Datapack;


    // Private fields.
    private readonly string _sourceDirPath;
    private readonly string _destDirPath;


    // Constructors.
    internal PackParser(string sourceDirectory, string? destDirectory)
    {
        if (sourceDirectory == null)
        {
            throw new ArgumentNullException(nameof(sourceDirectory));
        }
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException(nameof(sourceDirectory));
        }
        _sourceDirPath = sourceDirectory;

        _destDirPath = destDirectory ?? _sourceDirPath;
        if (_destDirPath == string.Empty)
        {
            _destDirPath = _sourceDirPath;
        }
        if (!Directory.Exists(_destDirPath))
        {
            throw new DirectoryNotFoundException(nameof(_destDirPath));
        }
    }


    // Static methods.
    internal static DataPack ParsePack(string sourceDirectory, string? destDirectory)
    {
        PackParser Parser = new(sourceDirectory, destDirectory);
        return Parser.ParsePack();
    }

    
    // Private methods.
    private DataPack ParsePack()
    {
        Datapack = new();
        
        foreach (string sourceFile in Directory.GetFiles(_sourceDirPath, "*.kgvl", SearchOption.AllDirectories))
        {
            SourceFileParser FileParser = new(sourceFile);
            FileParser.ParseFile();
        }

        return Datapack;
    }
}