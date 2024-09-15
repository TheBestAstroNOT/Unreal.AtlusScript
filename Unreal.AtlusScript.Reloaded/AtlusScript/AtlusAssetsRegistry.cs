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
        if (Directory.Exists(mod.BaseAssetsDir) == false)
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(mod.BaseAssetsDir, "*.*", SearchOption.AllDirectories))
        {
            if (file.StartsWith(mod.AstreaAssetsDir))
            {
                this.AddAssetFile(file, AssetMode.Astrea);
            }
            else
            {
                this.AddAssetFile(file, AssetMode.Default);
            }
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
        foreach (var file in Directory.EnumerateFiles(assetsDir, "*.*", SearchOption.AllDirectories))
        {
            this.AddAssetFile(file, mode);
        }
    }

    private void AddAssetFile(string file, AssetMode mode)
    {
        var ext = Path.GetExtension(file);
        if (ext.Equals(".msg", StringComparison.OrdinalIgnoreCase))
        {
            var msgAsset = new FileAssetContainer(this.compiler, file) { Mode = mode };
            msgAsset.Sync();

            this.assetContainers.Add(msgAsset);
            Log.Information($"Registered MSG ({mode}): {msgAsset.Name}");
        }
        else if (ext.Equals(".flow", StringComparison.OrdinalIgnoreCase))
        {
            var flowAsset = new FileAssetContainer(this.compiler, file) { Mode = mode };
            flowAsset.Sync();

            this.assetContainers.Add(flowAsset);
            Log.Information($"Registered BF ({mode}): {flowAsset.Name}");
        }
    }

    public void AddAsset(string name, string content, AssetType type) => this.AddAsset(name, content, type, AssetMode.Default);

    public void AddAsset(string name, string content, AssetType type, AssetMode mode)
    {
        var asset = new TextAssetContainer(this.compiler, name, type == AssetType.BF, content) { Mode = mode };
        asset.Sync();

        this.assetContainers.Add(asset);
    }
}
