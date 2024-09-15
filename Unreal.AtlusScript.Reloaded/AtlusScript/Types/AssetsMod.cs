namespace Unreal.AtlusScript.Reloaded.AtlusScript.Types;

public record AssetsMod(string ModId, string ModDir)
{
    public string DefaultAssetsDir { get; } = Path.Join(ModDir, "ue-atlus-script");

    public string AstreaAssetsDir { get; } = Path.Join(ModDir, "ue-atlus-script", "astrea");
}