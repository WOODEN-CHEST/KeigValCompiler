﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member.Code;

internal class VariableAssignmentStatement : Statement
{
    // Internal fields.
    internal override IEnumerable<Statement> SubStatements => Assignments
        .Select(assignment => assignment.Value)
        .Where(statement => statement != null)!;

    internal Identifier? Type { get; set; }
    internal bool IsDeclaration { get; set; } = false;
    internal VariableAssignmentCollection Assignments { get; } = new();


    // Constructors.
    internal VariableAssignmentStatement(Identifier? type, bool isDeclaration)
    {
        Type = type;
        IsDeclaration = isDeclaration;
    }
}