using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompilerTest;

/* One test which failed, and why, in enough detail to find what went wrong without running it again. */
public record class FailedTestResult(string TestName, string Reason);