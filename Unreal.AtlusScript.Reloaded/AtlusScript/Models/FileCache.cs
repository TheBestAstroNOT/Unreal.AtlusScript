using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Unreal.AtlusScript.Interfaces;

namespace Unreal.AtlusScript.Reloaded.AtlusScript.Models
{
    public class FileCacheRegistry
    {
        /// <summary>
        /// Version of mod this cache is associated with.
        /// </summary>
        public string Version { get; set; } = null!;

        /// <summary>
        /// Time it takes for merged files to expire.
        /// </summary>
        [JsonIgnore]
        public TimeSpan Expiration = TimeSpan.FromDays(28);
        /// <summary>
        /// Folder where this cache is contained.
        /// </summary>
        [JsonIgnore]
        public string CacheFolder = null!;

        [JsonIgnore]
        public string RootFolder = null!;

        [JsonIgnore]
        public string ModsFolder = null!;

        /// <summary>
        /// Not for direct access. Public for serializer only (needed for source generation).
        /// Map of relative path to individual file.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public ConcurrentDictionary<string, List<CachedFile>> KeyToFile { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public FileCacheRegistry(string cacheFolder) { CacheFolder = cacheFolder; }

        /// <summary> Serializer use only. Do not use directly. </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public FileCacheRegistry() { }

        //Always iterate list backwards because we remove items from it.
        public bool TryGetCachedContent(CachedFile file, int arrIndex, out byte[]? contents)
        {
            if(file.IsExpired(DateTime.UtcNow, Expiration) || !file.IsCacheFresh(ModsFolder))
            {
                contents = null;
                if (KeyToFile.TryGetValue(file.sourceModID, out var fileList))
                {
                    File.Delete(Path.Combine(CacheFolder, file.sourceModID, (file.language.ToString() != "UNIVERSAL") ? file.language.ToString().ToLower() : string.Empty, file.assetMode.ToString().ToLower(), file.hashedName.ToLower()));
                    fileList.RemoveAt(arrIndex);
                    Save();
                }
                return false;
            }
            if(file.TryGetValidCache(CacheFolder, out contents))
            {
                file.lastAccessed = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        public bool TryGetCacheByModID(string modID, out Dictionary<FileIdentifier, Tuple<string, ReadOnlyMemory<byte>>> cacheContent)
        {
            cacheContent = new();
            if(KeyToFile.TryGetValue(modID, out var fileList))
            {
                cacheContent = new Dictionary<FileIdentifier, Tuple<string, ReadOnlyMemory<byte>>>(fileList.Count);
                for (int i = fileList.Count - 1; i >= 0; i--)
                {
                    var file = fileList[i];
                    if (TryGetCachedContent(file, i, out var contents))
                    {
                        var identifier = new FileIdentifier(file.sourceName, file.language, file.assetMode);
                        if (!cacheContent.ContainsKey(identifier))
                        {
                            cacheContent[identifier] = new(file.sourceDirectory, contents!);
                        }
                        else
                        {
                            // Implemented till i figure out a better way to do this while generating cache.
                            Log.Warning($"Duplicate file identifier found in cache: {identifier.Name} for mod {modID}. Deleting cache!");
                            File.Delete(Path.Combine(CacheFolder, file.sourceModID, (file.language.ToString() != "UNIVERSAL") ? file.language.ToString().ToLower() : string.Empty, file.assetMode.ToString().ToLower(), file.hashedName.ToLower()));
                            fileList.RemoveAt(i);
                            Save();
                        }
                    }
                }
                return true;
            }
            return false;
        }
        public void AddCacheByModID(string modID, CachedFile file)
        {
            if (KeyToFile.TryGetValue(modID, out var fileList))
            {
                fileList.Add(file);
            }
            else
            {
                KeyToFile[modID] = new List<CachedFile> { file };
            }
        }

        public void Save()
        {
            var jsonPath = Path.Combine(CacheFolder, "CacheRegistry.json");

            // Optionally create directory if not exists
            Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true // or false if size matters
            });

            File.WriteAllText(jsonPath, json);
        }
    }
}

public readonly record struct FileIdentifier(
    string Name,
    ESystemLanguage Language = ESystemLanguage.UNIVERSAL,
    AssetMode AssetMode = AssetMode.Default
);

public sealed class CachedFile
{
    /// <summary>
    /// Language of the file. (Universal, English, Japanese, etc.)
    /// </summary>
    public ESystemLanguage language { get; set; } = ESystemLanguage.UNIVERSAL;

    /// <summary>
    /// Asset mode of the file. (Default, Astrea)
    /// </summary>
    public AssetMode assetMode { get; set; } = AssetMode.Default;

    /// <summary>
    /// Mod ID of the source file.
    /// </summary>
    public string sourceModID { get; set; } = string.Empty;

    /// <summary>
    /// Name of the source file
    /// </summary>
    public string sourceName { get; set; } = string.Empty;

    /// <summary>
    /// Path to directory containing the source file relative to the mod root.
    /// </summary>
    public string sourceDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Hashed name of the file, used for cache file name.
    /// </summary>
    public string hashedName { get; set; } = string.Empty;

    public DateTime sourceLastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Time file was last accessed. Used for flushing old files from cache.
    /// </summary>
    public DateTime lastAccessed { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Returns true if item is expired.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <param name="expirationTime">How long before item should expire.</param>
    public bool IsExpired(DateTime now, TimeSpan expirationTime)
    {
        var timeSinceLastAccess = now - lastAccessed;
        return timeSinceLastAccess >= expirationTime;
    }

    /// <summary>
    /// Returns true if the cache is up to date.
    /// <paramref name="modsRoot"> Path to the Reloaded mods directory.</paramref>
    /// </summary>
    public bool IsCacheFresh(string modsRoot)
    {
        var sourcePath = Path.Combine(modsRoot, sourceModID, sourceDirectory, sourceName);
        try
        {
            if (File.Exists(sourcePath) && File.GetLastWriteTimeUtc(sourcePath) <= sourceLastModified)
            {
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to check if cache is fresh for {sourcePath}, Exception: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Returns true if cache is valid and contents are returned, else returns false.
    /// </summary>
    /// <param name="cacheRoot">Path to the cache folder</param>
    /// <param name="contents">Returns cached content in the form of byte[]?.</param>
    /// <returns></returns>
    public bool TryGetValidCache(string cacheRoot, out byte[]? contents)
    {
        
        string cachePath = Path.Combine(cacheRoot, sourceModID, (language.ToString() != "UNIVERSAL") ? language.ToString().ToLower() : string.Empty, assetMode.ToString().ToLower(), hashedName.ToLower());
        try {
            contents = File.ReadAllBytes(cachePath);
            return true;
        }
        catch (Exception ex)
        {
            contents = null;
            Log.Error($"Failed to get cache, Exception: {ex}");
            return false;
        }
    }
}
