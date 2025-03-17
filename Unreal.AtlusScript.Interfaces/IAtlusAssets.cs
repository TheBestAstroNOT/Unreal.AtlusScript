namespace Unreal.AtlusScript.Interfaces;

public interface IAtlusAssets
{
    /// <summary>
    /// Add a folder to register Atlus assets from.
    /// For default (Xrd777) game scripts only.
    /// </summary>
    /// <param name="assetsDir">Assets folder.</param>
    [Obsolete("Use RegisterAssetsFolder instead.")]
    void AddAssetsFolder(string assetsDir);

    /// <summary>
    /// Add a folder to register Atlus assets from.
    /// </summary>
    /// <param name="assetsDir">Assets folder.</param>
    /// <param name="mode">Registration mode.</param>
    /// <param name="lang">Asset language.</param>
    void RegisterAssetsFolder(string assetsDir, AssetMode mode, ESystemLanguage lang);

    /// <summary>
    /// Add a folder to register Atlus assets from.
    /// </summary>
    /// <param name="assetsDir">Assets folder.</param>
    /// <param name="lang">Asset language.</param>
    void RegisterAssetsFolder(string assetsDir, ESystemLanguage lang);

    /// <summary>
    /// Add a folder to register Atlus assets from.
    /// </summary>
    /// <param name="assetsDir">Assets folder.</param>
    /// <param name="mode">Registration mode.</param>
    [Obsolete("Use RegisterAssetsFolder instead.")]
    void AddAssetsFolder(string assetsDir, AssetMode mode);

    /// <summary>
    /// Add an asset with the given name and content.
    /// For default (Xrd777) game scripts only.
    /// </summary>
    /// <param name="name">Asset name.</param>
    /// <param name="content">Text content of asset.</param>
    /// <param name="type">Asset type.</param>
    void AddAsset(string name, string content, AssetType type);

    /// <summary>
    /// Add an asset with the given name and content.
    /// </summary>
    /// <param name="name">Asset name.</param>
    /// <param name="content">Text content of asset.</param>
    /// <param name="type">Asset type.</param>
    /// <param name="mode">Asset mode.</param>
    void AddAsset(string name, string content, AssetType type, AssetMode mode);
}

public enum AssetType
{
    BMD,
    BF,
}

public enum AssetMode
{
    Default,
    Astrea,
    Both,
}

public enum ESystemLanguage : byte
{
    JA = 0,
    EN = 1,
    FR = 2,
    IT = 3,
    DE = 4,
    ES = 5,
    ZH_HANS = 6,
    ZH_HANT = 7,
    KO = 8,
    RU = 9,
    PT = 10,
    TR = 11,
    PL = 12,
};