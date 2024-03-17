namespace Unreal.AtlusScript.Reloaded.AtlusScript.Types;

public record AssetsMod(string ModId, string ModDir)
{
    public string AssetsDir { get; } = Path.Join(ModDir, "ue-atlus-script");
}