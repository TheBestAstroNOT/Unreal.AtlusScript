using System.Diagnostics.CodeAnalysis;
using Unreal.AtlusScript.Interfaces;
using Unreal.AtlusScript.Reloaded.AtlusScript.Models;
using Unreal.AtlusScript.Reloaded.AtlusScript.Types;

namespace Unreal.AtlusScript.Reloaded.AtlusScript;

internal unsafe class AtlusAssetsRegistry : IAtlusAssets
{
    private readonly AtlusAssetCompiler compiler;

    private readonly Dictionary<ESystemLanguage, List<BaseAssetContainer>> assetContainers = new() 
    {
        {ESystemLanguage.EN, []},
        {ESystemLanguage.JP, []},
        {ESystemLanguage.KO, []},
        {ESystemLanguage.ZH_HANS, []},
        {ESystemLanguage.ZH_HANT, []},
        {ESystemLanguage.DE, []},
        {ESystemLanguage.FR, []},
        {ESystemLanguage.IT, []},
        {ESystemLanguage.PL, []},
        {ESystemLanguage.PT, []},
        {ESystemLanguage.RU, []},
        {ESystemLanguage.ES, []},
        {ESystemLanguage.TR, []}
    };

    public AtlusAssetsRegistry(AtlusAssetCompiler compiler)
    {
        this.compiler = compiler;
    }

    public void RegisterMod(AssetsMod mod)
    {
        Log.Information($"Registering assets from: {mod.ModId}");
        if (Directory.Exists(mod.BaseAssetsDir))
        {
            foreach (var topdir in Directory.EnumerateDirectories(mod.BaseAssetsDir, "*", SearchOption.TopDirectoryOnly))
            {
                if (topdir.StartsWith(mod.AstreaAssetsDir))
                {
                    foreach (var dir in Directory.EnumerateDirectories(topdir, "*", SearchOption.TopDirectoryOnly))
                    {
                        ESystemLanguage dirLang = GetFileLang(mod.AstreaAssetsDir, dir);
                        foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
                        {
                            this.AddAssetFile(file, AssetMode.Astrea, dirLang, true);
                        }
                    }
                }
                else
                {
                    ESystemLanguage dirLang = GetFileLang(mod.BaseAssetsDir, topdir);
                    foreach (var file in Directory.EnumerateFiles(topdir, "*.*", SearchOption.AllDirectories))
                    {
                        this.AddAssetFile(file, AssetMode.Default, dirLang, true);
                    }
                }
            }
            foreach (var basedirfile in Directory.EnumerateFiles(mod.BaseAssetsDir, "*.*", SearchOption.TopDirectoryOnly))
            {
                this.AddAssetFile(basedirfile, AssetMode.Default, ESystemLanguage.EN, true);
            }
        }
        if(Directory.Exists(mod.ModernBaseAssetsDir))
        {
            foreach (var topdir in Directory.EnumerateDirectories(mod.ModernBaseAssetsDir, "*", SearchOption.TopDirectoryOnly))
            {
                if (topdir.StartsWith(mod.ModernAstreaAssetsDir))
                {
                    foreach (var dir in Directory.EnumerateDirectories(topdir, "*", SearchOption.TopDirectoryOnly))
                    {
                        ESystemLanguage dirLang = GetFileLang(mod.ModernAstreaAssetsDir, dir);
                        foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
                        {
                            this.AddAssetFile(file, AssetMode.Astrea, dirLang, false);
                        }
                    }
                }
                else
                {
                    ESystemLanguage dirLang = GetFileLang(mod.ModernBaseAssetsDir, topdir);
                    foreach (var file in Directory.EnumerateFiles(topdir, "*.*", SearchOption.AllDirectories))
                    {
                        this.AddAssetFile(file, AssetMode.Default, dirLang, false);
                    }
                }
            }
            foreach (var basedirfile in Directory.EnumerateFiles(mod.ModernBaseAssetsDir, "*.*", SearchOption.TopDirectoryOnly))
            {
                this.AddAssetFile(basedirfile, AssetMode.Default, ESystemLanguage.EN, false);
            }
        }   
    }

    private static ESystemLanguage GetFileLang(string assetFolder, string file)
    {
        var topPath = Path.GetRelativePath(assetFolder, file).Split(Path.DirectorySeparatorChar).FirstOrDefault();
        if (!string.IsNullOrEmpty(topPath) && Enum.TryParse<ESystemLanguage>(topPath, true, out var lang))
        {
            return lang;
        }
        return ESystemLanguage.EN;
    }

    public bool TryGetAsset(AssetMode mode, string assetName, [NotNullWhen(true)]out byte[]? assetData, ESystemLanguage currentAssetLang)
    {
        var asset = GetAssetForMode(mode, assetName, currentAssetLang);

        if (asset == null || asset.Data == null)
        {
            assetData = null;
            return false;
        }

        assetData = asset.Data;
        return true;
    }

    private BaseAssetContainer? GetAssetForMode(AssetMode mode, string assetName, ESystemLanguage currentAssetLang)
    {
        var asset = assetContainers[currentAssetLang].FirstOrDefault(a => a.Name == assetName && a.Mode == mode);
        if (asset == null)
        {
            asset = assetContainers[ESystemLanguage.EN].FirstOrDefault(a => a.Name == assetName && a.Mode == mode);
            if(asset != null && !asset.isUniversal)
            {
                return null;
            }
        }
        return asset;
    }

    [Obsolete("Use RegisterAssetsFolder instead.")]
    public void AddAssetsFolder(string assetsDir) => this.AddAssetsFolder(assetsDir, AssetMode.Default);

    [Obsolete("Use RegisterAssetsFolder instead.")]
    public void AddAssetsFolder(string assetsDir, AssetMode mode)
    {
        // Process folder.
        foreach (var file in Directory.EnumerateFiles(assetsDir, "*.*", SearchOption.AllDirectories))
        {
            this.AddAssetFile(file, mode, ESystemLanguage.EN, true);
        }
    }

    public void RegisterAssetsFolder(string assetsDir, ESystemLanguage lang, bool isUniversal) => this.RegisterAssetsFolder(assetsDir, AssetMode.Default, lang, isUniversal);

    public void RegisterAssetsFolder(string assetsDir, AssetMode mode, ESystemLanguage currentAssetLang, bool isUniversal)
    {
        // Process folder.
        foreach (var file in Directory.EnumerateFiles(assetsDir, "*.*", SearchOption.AllDirectories))
        {
            this.AddAssetFile(file, mode, currentAssetLang, isUniversal);
        }
    }

    private void AddAssetFile(string file, AssetMode mode, ESystemLanguage currentAssetLang, bool isUniversal)
    {
        var ext = Path.GetExtension(file);
        if (ext.Equals(".msg", StringComparison.OrdinalIgnoreCase))
        {
            var msgAsset = new FileAssetContainer(this.compiler, file, isUniversal) { Mode = mode };
            msgAsset.Sync();
            this.assetContainers[currentAssetLang].Add(msgAsset);
            Log.Verbose($"Registered MSG ({mode}): {msgAsset.Name}, Language: {currentAssetLang}");
        }
        else if (ext.Equals(".flow", StringComparison.OrdinalIgnoreCase))
        {
            var flowAsset = new FileAssetContainer(this.compiler, file, isUniversal) { Mode = mode };
            flowAsset.Sync();
            this.assetContainers[currentAssetLang].Add(flowAsset);
            Log.Verbose($"Registered BF ({mode}): {flowAsset.Name}, Language: {currentAssetLang}");
        }
    }

    public void AddAsset(string name, string content, AssetType type) => this.AddAsset(name, content, type, AssetMode.Default);

    public void AddAsset(string name, string content, AssetType type, AssetMode mode)
    {
        var asset = new TextAssetContainer(this.compiler, name, type == AssetType.BF, content) { Mode = mode };
        asset.Sync();
        this.assetContainers[ESystemLanguage.EN].Add(asset);
    }
}
