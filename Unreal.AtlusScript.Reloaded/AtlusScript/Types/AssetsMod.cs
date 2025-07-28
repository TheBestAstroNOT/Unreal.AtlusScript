namespace Unreal.AtlusScript.Reloaded.AtlusScript.Types;

public record AssetsMod(string ModId, string ModDir)
{
    public string BaseAssetsDir { get; } = Path.Join(ModDir, "ue-atlus-script");

    public string ModernBaseAssetsDir { get; } = Path.Join(ModDir, "modern-atlus-script");

    public string ModernAstreaAssetsDir { get; } = Path.Join(ModDir, "modern-atlus-script", "astrea");

    public string AstreaAssetsDir { get; } = Path.Join(ModDir, "ue-atlus-script", "astrea");
}