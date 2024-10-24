using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal interface IPackMemberExtender
{
    // Fields/
    IEnumerable<Identifier> ExtendedMembers { get; }

    // Methods.
    void AddExtendedMember(Identifier identifier);
    void RemoveExtendedMember(Identifier identifier);
}