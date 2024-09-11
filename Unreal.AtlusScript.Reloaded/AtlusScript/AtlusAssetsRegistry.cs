using System.Diagnostics.CodeAnalysis;
using Unreal.AtlusScript.Interfaces;
using Unreal.AtlusScript.Reloaded.AtlusScript.Models;
using Unreal.AtlusScript.Reloaded.AtlusScript.Types;

namespace Unreal.AtlusScript.Reloaded.AtlusScript;

internal unsafe class AtlusAssetsRegistry : IAtlusAssets
{
    private readonly AtlusAssetCompiler compiler;
    private readonly List<BaseAssetContainer> assetContainers = [];

    public AtlusAssetsRegistry(AtlusAssetCompiler compiler)
    {
        this.compiler = compiler;
    }

    public void RegisterMod(AssetsMod mod)
    {
        Log.Information($"Registering assets from: {mod.ModId}");
        if (Directory.Exists(mod.DefaultAssetsDir))
        {
            this.AddAssetsFolder(mod.DefaultAssetsDir, AssetMode.Default);
        }

        if (Directory.Exists(mod.AstreaAssetsDir))
        {
            this.AddAssetsFolder(mod.AstreaAssetsDir, AssetMode.Astrea);
        }
    }

    public bool TryGetAsset(AssetMode mode, string assetName, [NotNullWhen(true)]out byte[]? assetData)
    {
        var asset = GetAssetForMode(mode, assetName);

        if (asset == null || asset.Data == null)
        {
            assetData = null;
            return false;
        }

        assetData = asset.Data;
        return true;
    }

    private BaseAssetContainer? GetAssetForMode(AssetMode mode, string assetName)
        => this.assetContainers.FirstOrDefault(x => (x.Mode == mode || x.Mode == AssetMode.Both) && x.Name.Equals(assetName, StringComparison.OrdinalIgnoreCase));

    public void AddAssetsFolder(string assetsDir) => this.AddAssetsFolder(assetsDir, AssetMode.Default);

    public void AddAssetsFolder(string assetsDir, AssetMode mode)
    {
        // Process folder.
        foreach (var msgFile in Directory.EnumerateFiles(assetsDir, "*.msg"))
        {
            var msgAsset = new FileAssetContainer(this.compiler, msgFile) { Mode = mode };
            msgAsset.Sync();

            this.assetContainers.Add(msgAsset);
            Log.Information($"Registered MSG ({mode}): {msgAsset.Name}");
        }

        foreach (var flowFile in Directory.EnumerateFiles(assetsDir, "*.flow"))
        {
            var flowAsset = new FileAssetContainer(this.compiler, flowFile) { Mode = mode };
            flowAsset.Sync();

            this.assetContainers.Add(flowAsset);
            Log.Information($"Registered BF ({mode}): {flowAsset.Name}");
        }

        // Recursively process nested folders.
        foreach (var dir in Directory.EnumerateDirectories(assetsDir))
        {
            this.AddAssetsFolder(dir, mode);
        }
    }

    public void AddAsset(string name, string content, AssetType type)
        => this.AddAsset(name, content, type, AssetMode.Default);

    public void AddAsset(string name, string content, AssetType type, AssetMode mode)
        => this.assetContainers.Add(new TextAssetContainer(this.compiler, name, type == AssetType.BF, content) { Mode = mode });
}
