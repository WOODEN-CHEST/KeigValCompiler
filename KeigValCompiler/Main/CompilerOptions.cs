using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Main;

internal class CompilerOptions
{
    // Internal fields.
    internal string SourceDirectory { get; private init; }
    internal string DestinationDirectory { get; private init; }


    // Constructors.
    internal CompilerOptions(string[] args)
    {
        if (args.Length == 0)
        {
            throw new CommandlineArgumentException("Zero arguments, expected something other than this deep, empty void.");
        }

        SourceDirectory = args[0];
        if (!Directory.Exists(SourceDirectory))
        {
            throw new CommandlineArgumentException($"Source directory \"{SourceDirectory}\" not found.");
        }


        DestinationDirectory = SourceDirectory;
        if (args.Length >= 2)
        {
            DestinationDirectory = args[1];
            Directory.CreateDirectory(DestinationDirectory);
        }
    }
}