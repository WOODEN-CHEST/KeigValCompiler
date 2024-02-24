using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician;

internal class PackContentException : Exception
{
    internal PackContentException(string message) : base($"Invalid pack content! ${message}") { }
}