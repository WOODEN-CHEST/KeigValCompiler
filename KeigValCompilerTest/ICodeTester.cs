using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompilerTest;

public interface ICodeTester
{
    // Fields.
    /* What is tested, as the results are printed under. */
    string Name { get; }


    // Methods.
    TestResults Test();
}