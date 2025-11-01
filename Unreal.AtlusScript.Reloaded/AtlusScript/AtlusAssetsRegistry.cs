using Reloaded.Mod.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Unreal.AtlusScript.Interfaces;
using Unreal.AtlusScript.Reloaded.AtlusScript.Models;
using Unreal.AtlusScript.Reloaded.AtlusScript.Types;

namespace Unreal.AtlusScript.Reloaded.AtlusScript;

internal unsafe class AtlusAssetsRegistry : IAtlusAssets
{
    private readonly AtlusAssetCompiler compiler;
    private readonly IModLoader modLoader;
    private readonly FileCacheRegistry fileCache;
    private readonly SHA1 sha1;
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
        {ESystemLanguage.TR, []},
        {ESystemLanguage.Any, []}
    };

    public AtlusAssetsRegistry(IModLoader modLoader, AtlusAssetCompiler compiler, FileCacheRegistry fileCache)
    {
        this.modLoader = modLoader;
        this.compiler = compiler;
        this.fileCache = fileCache;
        sha1 = System.Security.Cryptography.SHA1.Create();
    }

    public void RegisterMod(AssetsMod mod)
    {
        Log.Information($"Registering assets from: {mod.ModId}");
        fileCache.TryGetCacheByModId(mod.ModId.ToLower(), out var cacheContent);
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
                            var identifier = new FileIdentifier(Path.GetFileName(file), dirLang, AssetMode.Astrea);
                            if (cacheContent.ContainsKey(identifier))
                            {
                                if (!LoadAssetCache(identifier, cacheContent[identifier], mod))
                                {
                                    cacheContent.Remove(identifier);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            this.AddAssetFile(file, AssetMode.Astrea, dirLang, mod);
                        }
                    }
                }
                else
                {
                    ESystemLanguage dirLang = GetFileLang(mod.BaseAssetsDir, topdir);
                    foreach (var file in Directory.EnumerateFiles(topdir, "*.*", SearchOption.AllDirectories))
                    {
                        var identifier = new FileIdentifier(Path.GetFileName(file), dirLang, AssetMode.Default);
                        if (cacheContent.ContainsKey(identifier))
                        {
                            if (!LoadAssetCache(identifier, cacheContent[identifier], mod))
                            {
                                cacheContent.Remove(identifier);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        this.AddAssetFile(file, AssetMode.Default, dirLang, mod);
                    }
                }
            }
            foreach (var basedirfile in Directory.EnumerateFiles(mod.BaseAssetsDir, "*.*", SearchOption.TopDirectoryOnly))
            {
                var identifier = new FileIdentifier(Path.GetFileName(basedirfile), ESystemLanguage.Any, AssetMode.Default);
                if (cacheContent.ContainsKey(identifier))
                {
                    if (!LoadAssetCache(identifier, cacheContent[identifier], mod))
                    {
                        cacheContent.Remove(identifier);
                    }
                    else
                    {
                        continue;
                    }
                }
                this.AddAssetFile(basedirfile, AssetMode.Default, ESystemLanguage.Any, mod);
            }
            if (Directory.Exists(mod.AstreaAssetsDir))
            {
                foreach (var astreadirfile in Directory.EnumerateFiles(mod.AstreaAssetsDir, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var identifier = new FileIdentifier(Path.GetFileName(astreadirfile), ESystemLanguage.Any, AssetMode.Astrea);
                    if (cacheContent.ContainsKey(identifier))
                    {
                        if (!LoadAssetCache(identifier, cacheContent[identifier], mod))
                        {
                            cacheContent.Remove(identifier);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    this.AddAssetFile(astreadirfile, AssetMode.Astrea, ESystemLanguage.Any, mod);
                }
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
        return ESystemLanguage.Any;
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
            asset = assetContainers[ESystemLanguage.Any].FirstOrDefault(a => a.Name == assetName && (a.Mode == mode || a.Mode == AssetMode.Both));
        }
        return asset;
    }

    public void AddAssetsFolder(string assetsDir) => this.AddAssetsFolder(assetsDir, AssetMode.Default);

    public void AddAssetsFolder(string assetsDir, AssetMode mode)
    {
        foreach (var dir in Directory.EnumerateDirectories(assetsDir, "*", SearchOption.AllDirectories))
        {
            ESystemLanguage dirLang = GetFileLang(assetsDir, dir);
            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                var identifier = new FileIdentifier(Path.GetFileName(file), dirLang, mode);
                this.AddAssetFile(file, mode, dirLang);
            }
        }
        foreach (var file in Directory.EnumerateFiles(assetsDir, "*.*", SearchOption.TopDirectoryOnly))
        {
            this.AddAssetFile(file, mode, ESystemLanguage.Any);
        }
    }

    public void AddAssetsFolderCached(string assetsDir, string modId) => this.AddAssetsFolderCached(assetsDir, modId, AssetMode.Default);

    public void AddAssetsFolderCached(string assetsDir, string modId, AssetMode mode)
    {
        fileCache.TryGetCacheByModId(modId, out var cacheContent);
        foreach (var dir in Directory.EnumerateDirectories(assetsDir, "*", SearchOption.AllDirectories))
        {
            ESystemLanguage dirLang = GetFileLang(assetsDir, dir);
            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                var identifier = new FileIdentifier(Path.GetFileName(file), dirLang, mode);
                if (cacheContent.ContainsKey(identifier))
                {
                    if (!LoadAssetCache(identifier, cacheContent[identifier], modId))
                    {
                        cacheContent.Remove(identifier);
                    }
                    else
                    {
                        continue;
                    }
                }
                this.AddAssetFile(file, mode, dirLang, modId);
                
            }
        }
        foreach (var file in Directory.EnumerateFiles(assetsDir, "*.*", SearchOption.TopDirectoryOnly))
        {
            var identifier = new FileIdentifier(Path.GetFileName(file), ESystemLanguage.Any, mode);
            if (cacheContent.ContainsKey(identifier))
            {
                if (!LoadAssetCache(identifier, cacheContent[identifier], modId))
                {
                    cacheContent.Remove(identifier);
                }
                else
                {
                    continue;
                }
            }
            this.AddAssetFile(file, mode, ESystemLanguage.Any, modId);
        }
    }

    private void AddAssetFile(string file, AssetMode mode, ESystemLanguage currentAssetLang, string modId) => AddAssetFile(file, mode, currentAssetLang, new AssetsMod(modId, this.modLoader.GetDirectoryForModId(modId)));

    private void AddAssetFile(string file, AssetMode mode, ESystemLanguage currentAssetLang, AssetsMod? modData = null)
    {
        var ext = Path.GetExtension(file);
        if (ext.Equals(".msg", StringComparison.OrdinalIgnoreCase))
        {
            var msgAsset = new FileAssetContainer(this.compiler, file) { Mode = mode };
            msgAsset.Sync();
            this.assetContainers[currentAssetLang].Add(msgAsset);
            if (modData != null && msgAsset.Data != null)
            {
                byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileName(file)));
                string hashedName = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                fileCache.AddCacheByModID(modData.ModId, new CachedFile
                {
                    sourceName = Path.GetFileName(file),
                    sourceModID = modData.ModId,
                    sourceDirectory = Path.GetDirectoryName(Path.GetRelativePath(modData.ModDir, file)) ?? string.Empty,
                    language = currentAssetLang,
                    assetMode = mode,
                    hashedName = hashedName
                });
                string filePath = GetHashedCachePath(fileCache.CacheFolder, hashedName, modData, mode, currentAssetLang);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);
                File.WriteAllBytes(filePath, msgAsset.Data);
                fileCache.Save();
            }
            Log.Debug($"Registered MSG ({mode}): {msgAsset.Name} / language: {currentAssetLang}");
        }
        else if (ext.Equals(".flow", StringComparison.OrdinalIgnoreCase))
        {
            var flowAsset = new FileAssetContainer(this.compiler, file) { Mode = mode };
            flowAsset.Sync();
            this.assetContainers[currentAssetLang].Add(flowAsset);
            if (modData != null && flowAsset.Data != null)
            {
                byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileName(file)));
                string hashedName = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                fileCache.AddCacheByModID(modData.ModId, new CachedFile
                {
                    sourceName = Path.GetFileName(file),
                    sourceModID = modData.ModId,
                    sourceDirectory = Path.GetDirectoryName(Path.GetRelativePath(modData.ModDir, file)) ?? string.Empty,
                    language = currentAssetLang,
                    assetMode = mode,
                    hashedName = hashedName
                });
                string filePath = GetHashedCachePath(fileCache.CacheFolder, hashedName, modData, mode, currentAssetLang);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);
                File.WriteAllBytes(filePath, flowAsset.Data);
                fileCache.Save();
            }
            Log.Debug($"Registered BF ({mode}): {flowAsset.Name} / language: {currentAssetLang}");
        }
    }

    public static string GetHashedCachePath(string CacheRoot, string hashedname, AssetsMod modData, AssetMode mode, ESystemLanguage currentAssetLang) => GetHashedCachePath(CacheRoot, hashedname, modData.ModId, mode, currentAssetLang);
    public static string GetHashedCachePath(string CacheRoot, string hashedname, string modId, AssetMode mode, ESystemLanguage currentAssetLang)
    {
        return Path.Join(CacheRoot, modId.ToLower(), (currentAssetLang != ESystemLanguage.Any) ? currentAssetLang.ToString().ToLower() : string.Empty, mode.ToString().ToLower(), hashedname);
    }

    private bool LoadAssetCache(FileIdentifier identifier, Tuple<string, ReadOnlyMemory<byte>> data, string modId) => LoadAssetCache(identifier, data, new AssetsMod(modId, this.modLoader.GetDirectoryForModId(modId)));
    private bool LoadAssetCache(FileIdentifier identifier, Tuple<string, ReadOnlyMemory<byte>> data, AssetsMod mod)
    {
        Log.Verbose($"Loading cached asset: {identifier.Name} ");
        var ext = Path.GetExtension(identifier.Name);
        string filePath = Path.Join(mod.ModDir, data.Item1, identifier.Name);
        if (File.Exists(filePath)) {
            if (ext.Equals(".msg", StringComparison.OrdinalIgnoreCase))
            {
                var msgAsset = new FileAssetContainer(this.compiler, filePath) { Mode = identifier.AssetMode };
                msgAsset.SyncCache(data.Item2.ToArray());
                this.assetContainers[identifier.Language].Add(msgAsset);
                Log.Debug($"Registered MSG from Cache ({msgAsset.Mode}): {msgAsset.Name} / language: {identifier.Language}");
            }
            else if (ext.Equals(".flow", StringComparison.OrdinalIgnoreCase))
            {
                var flowAsset = new FileAssetContainer(this.compiler, filePath) { Mode = identifier.AssetMode };
                flowAsset.SyncCache(data.Item2.ToArray());
                this.assetContainers[identifier.Language].Add(flowAsset);
                Log.Debug($"Registered BF from Cache ({flowAsset.Mode}): {flowAsset.Name} / language: {identifier.Language}");
            }
            return true;
        }
        else
        {
            Log.Debug($"Failed to load asset from Cache ({identifier.AssetMode}): {identifier.Name} / language: {identifier.Language}");
            return false;
        }
    }

    public void AddAsset(string name, string content, AssetType type) => this.AddAsset(name, content, type, AssetMode.Default);

    public void AddAsset(string name, string content, AssetType type, AssetMode mode)
    {
        var asset = new TextAssetContainer(this.compiler, name, type == AssetType.BF, content) { Mode = mode };
        asset.Sync();
        this.assetContainers[ESystemLanguage.Any].Add(asset);
    }
}
