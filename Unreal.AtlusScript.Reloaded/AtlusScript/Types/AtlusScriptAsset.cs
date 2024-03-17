using System.Runtime.InteropServices;
using Unreal.ObjectsEmitter.Interfaces.Types;

namespace Unreal.AtlusScript.Reloaded.AtlusScript.Types;

[StructLayout(LayoutKind.Explicit, Size = 0x38)]
public unsafe struct UAtlusScriptAsset
{
    [FieldOffset(0x0000)] public UObject baseObj;
    [FieldOffset(0x0028)] public TArray<byte> mBuf;
}