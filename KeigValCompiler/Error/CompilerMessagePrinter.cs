using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Error;

internal class CompilerMessagePrinter
{
    // Methods.
    internal void PrintMessages(CompilerMessageCollection messages)
    {
        ArgumentNullException.ThrowIfNull(messages, nameof(messages));

        foreach (CompilerMessage Message in messages.GetSortedMessages())
        {
            Console.WriteLine(Message.ToString());
        }
    }

    internal void PrintSummary(CompilerMessageCollection messages)
    {
        ArgumentNullException.ThrowIfNull(messages, nameof(messages));

        if (messages.Count == 0)
        {
            return;
        }

        Console.WriteLine($"{messages.ErrorCount} {GetCountedNoun(messages.ErrorCount, "error")}, "
            + $"{messages.WarningCount} {GetCountedNoun(messages.WarningCount, "warning")}.");
    }


    // Private methods.
    private string GetCountedNoun(int count, string noun)
    {
        return count == 1 ? noun : $"{noun}s";
    }
}