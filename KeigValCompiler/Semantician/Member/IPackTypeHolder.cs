using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal interface IPackTypeHolder
{
    // Fields.
    PackClass[] Classes { get; }
    PackInterface[] Interfaces { get; }
    PackStruct[] Structs { get; }
    PackEnumeration[] Enums { get; }


    // Methods.
    void AddClass(PackClass packClass);
    void AddInterface(PackInterface packInterface);
    void AddStruct(PackStruct packStruct);
    void AddEnum(PackEnumeration packEnum);
    void RemoveClass(PackClass packClass);
    void RemoveInterface(PackInterface packInterface);
    void RemoveStruct(PackStruct packStruct);
    void RemoveEnum(PackEnumeration packEnum);
}