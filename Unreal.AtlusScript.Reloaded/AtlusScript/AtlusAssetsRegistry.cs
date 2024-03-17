using AtlusScriptLibrary.FlowScriptLanguage.Compiler;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using System.Runtime.InteropServices;
using Unreal.AtlusScript.Reloaded.AtlusScript.Types;
using Unreal.ObjectsEmitter.Interfaces.Types;

namespace Unreal.AtlusScript.Reloaded.AtlusScript;

internal unsafe class AtlusAssetsRegistry
{
    private readonly MessageScriptCompiler msgCompiler;
    private readonly FlowScriptCompiler flowCompiler;
    private readonly Dictionary<string, TArray<byte>> newAtlusAssets = new(StringComparer.OrdinalIgnoreCase);

    public AtlusAssetsRegistry(FlowScriptCompiler flowCompiler, MessageScriptCompiler msgCompiler)
    {
        this.flowCompiler = flowCompiler;
        this.msgCompiler = msgCompiler;
    }

    public void RegisterMod(AssetsMod mod)
    {
        if (Directory.Exists(mod.AssetsDir))
        {
            Log.Information($"Registering assets from: {mod.ModId}");
            ProcessAssetsFolder(mod.AssetsDir);
        }
    }

    public bool TryGetAsset(string assetName, out TArray<byte> asset)
        => newAtlusAssets.TryGetValue(assetName, out asset);

    public void ProcessAssetsFolder(string assetsDir)
    {
        // Process folder.
        foreach (var msgFile in Directory.EnumerateFiles(assetsDir, "*.msg"))
        {
            try
            {
                CompileBMD(msgFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to compile message script.\nFile: {msgFile}");
            }
        }

        foreach (var flowFile in Directory.EnumerateFiles(assetsDir, "*.flow"))
        {
            try
            {
                CompileBF(flowFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to compile flowscript.\nFile: {flowFile}");
            }
        }

        // Recursively process nested folders.
        foreach (var dir in Directory.EnumerateDirectories(assetsDir))
        {
            ProcessAssetsFolder(dir);
        }
    }

    private void CompileBMD(string msgFile)
    {
        if (msgCompiler.TryCompile(File.ReadAllText(msgFile), out var script))
        {
            using var ms = new MemoryStream();
            script.ToStream(ms);

            var objName = Path.GetFileNameWithoutExtension(msgFile);
            newAtlusAssets[objName] = TArrayFromMemStream(ms);
            Log.Information($"Registered BMD asset: {objName}");
        }
        else
        {
            throw new Exception();
        }
    }

    private void CompileBF(string flowFile)
    {
        if (this.flowCompiler.TryCompile(File.ReadAllText(flowFile), out var flow))
        {
            using var ms = new MemoryStream();
            flow.ToStream(ms);

            var objName = Path.GetFileNameWithoutExtension(flowFile);
            newAtlusAssets[objName] = TArrayFromMemStream(ms);
            Log.Information($"Registered BF asset: {objName}");
        }
        else
        {
            throw new Exception();
        }
    }

    private static TArray<byte> TArrayFromMemStream(MemoryStream ms)
    {
        var bytes = ms.ToArray();
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

        var array = new TArray<byte>()
        {
            Num = bytes.Length,
            Max = bytes.Length,
            AllocatorInstance = (byte*)handle.AddrOfPinnedObject(),
        };

        return array;
    }
}
