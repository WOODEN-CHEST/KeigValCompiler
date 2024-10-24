using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeigValCompiler.Semantician.Member;

internal interface IPackTypeHolder
{
    // Fields.
    IEnumerable<PackClass> Classes { get; }
    IEnumerable<PackClass> AllClasses { get; }
    IEnumerable<PackInterface> Interfaces { get; }
    IEnumerable<PackInterface> AllInterfaces { get; }
    IEnumerable<PackStruct> Structs { get; }
    IEnumerable<PackStruct> AllStructs { get; }
    IEnumerable<PackEnumeration> Enums { get; }
    IEnumerable<PackEnumeration> AllEnums { get; }
    IEnumerable<PackDelegate> Delegates { get; }
    IEnumerable<PackDelegate> AllDelegates { get; }
    IEnumerable<PackMember> Types { get; }
    IEnumerable<PackMember> AllTypes { get; }


    // Methods.
    void AddClass(PackClass packClass);
    void AddInterface(PackInterface packInterface);
    void AddStruct(PackStruct packStruct);
    void AddEnum(PackEnumeration packEnum);
    void AddDelegate(PackDelegate packDelegate);
    void RemoveClass(PackClass packClass);
    void RemoveInterface(PackInterface packInterface);
    void RemoveStruct(PackStruct packStruct);
    void RemoveEnum(PackEnumeration packEnum);
    void RemoveDelegate(PackDelegate packDelegate);
}