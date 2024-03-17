using AtlusScriptLibrary.FlowScriptLanguage.Compiler;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using System.Runtime.InteropServices;
using Unreal.AtlusScript.Interfaces;
using Unreal.AtlusScript.Reloaded.AtlusScript.Types;
using Unreal.ObjectsEmitter.Interfaces.Types;

namespace Unreal.AtlusScript.Reloaded.AtlusScript;

internal unsafe class AtlusAssetsRegistry : IAtlusAssets
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
            this.AddAssetsFolder(mod.AssetsDir);
        }
    }

    public bool TryGetAsset(string assetName, out TArray<byte> asset)
        => newAtlusAssets.TryGetValue(assetName, out asset);

    public void AddAssetsFolder(string assetsDir)
    {
        // Process folder.
        foreach (var msgFile in Directory.EnumerateFiles(assetsDir, "*.msg"))
        {
            try
            {
                this.AddAsset(Path.GetFileNameWithoutExtension(msgFile), File.ReadAllText(msgFile), AssetType.MSG);
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
                this.AddAsset(Path.GetFileNameWithoutExtension(flowFile), File.ReadAllText(flowFile), AssetType.FLOW);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to compile flowscript.\nFile: {flowFile}");
            }
        }

        // Recursively process nested folders.
        foreach (var dir in Directory.EnumerateDirectories(assetsDir))
        {
            this.AddAssetsFolder(dir);
        }
    }

    public void AddAsset(string name, string content, AssetType type)
    {
        switch (type)
        {
            case AssetType.MSG: this.CompileBMD(name, content); break;
            case AssetType.FLOW: this.CompileBF(name, content); break;
            default: break;
        }
    }

    private void CompileBMD(string assetName, string msgContent)
    {
        if (msgCompiler.TryCompile(msgContent, out var script))
        {
            using var ms = new MemoryStream();
            script.ToStream(ms);

            newAtlusAssets[assetName] = TArrayFromMemStream(ms);
            Log.Information($"Registered BMD asset: {assetName}");
        }
        else
        {
            throw new Exception();
        }
    }

    private void CompileBF(string assetName, string flowContent)
    {
        if (this.flowCompiler.TryCompile(flowContent, out var flow))
        {
            using var ms = new MemoryStream();
            flow.ToStream(ms);

            newAtlusAssets[assetName] = TArrayFromMemStream(ms);
            Log.Information($"Registered BF asset: {assetName}");
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
