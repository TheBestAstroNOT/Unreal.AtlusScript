namespace Unreal.AtlusScript.Interfaces;

public interface IAtlusAssets
{
    /// <summary>
    /// Add a folder to registry Atlus assets from.
    /// </summary>
    /// <param name="assetsDir">Assets folder.</param>
    void AddAssetsFolder(string assetsDir);

    /// <summary>
    /// Add an asset with the given name and content.
    /// </summary>
    /// <param name="name">Asset name.</param>
    /// <param name="content">Text content of asset.</param>
    /// <param name="type">Asset type.</param>
    void AddAsset(string name, string content, AssetType type);
}

public enum AssetType
{
    MSG,
    FLOW,
}
