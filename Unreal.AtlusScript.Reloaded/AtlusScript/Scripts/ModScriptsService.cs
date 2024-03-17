using System.Runtime.InteropServices;
using Unreal.AtlusScript.Reloaded.AtlusScript.Types;

namespace Unreal.AtlusScript.Reloaded.AtlusScript.Scripts;

internal unsafe class ModScriptsService
{
    private void UpdateScript(string editedMsg, UAtlusScriptAsset* bmd)
    {
        //if (this.msgCompiler.TryCompile(File.ReadAllText(editedMsg), out var script))
        //{
        //    using var ms = new MemoryStream();
        //    script.ToStream(ms);
        //    var bytes = ms.ToArray();
        //    var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        //    bmd->mBuf.Num = bytes.Length;
        //    bmd->mBuf.Max = bytes.Length;
        //    bmd->mBuf.AllocatorInstance = (byte*)handle.AddrOfPinnedObject();
        //    Log.Information("Updated script");
        //}
        //else
        //{
        //    Log.Error($"Failed to compile message script.\nFile: {editedMsg}");
        //}
    }
}
