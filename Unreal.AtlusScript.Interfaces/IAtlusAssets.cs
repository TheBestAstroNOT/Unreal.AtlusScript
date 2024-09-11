namespace Unreal.AtlusScript.Interfaces;

public interface IAtlusAssets
{
    /// <summary>
    /// Add a folder to register Atlus assets from.
    /// For default (Xrd777) game scripts only.
    /// </summary>
    /// <param name="assetsDir">Assets folder.</param>
    void AddAssetsFolder(string assetsDir);

    /// <summary>
    /// Add a folder to register Atlus assets from.
    /// </summary>
    /// <param name="assetsDir">Assets folder.</param>
    /// <param name="mode">Registration mode.</param>
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
