using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal interface IPackMemberExtender
{
    // Fields/
    /* Types rather than bare names, since a base type can have type arguments, as in IEquatable<int>. */
    IEnumerable<TypeTargetIdentifier> ExtendedMembers { get; }
    int ExtendedMemberCount { get; }


    // Methods.
    void AddExtendedMember(TypeTargetIdentifier type);
    void RemoveExtendedMember(TypeTargetIdentifier type);
}