namespace Unreal.AtlusScript.Reloaded.AtlusScript.Models;

internal class TextAssetContainer(AtlusAssetCompiler compiler, string name, bool isFlow, string source)
    : BaseAssetContainer(compiler, name, isFlow, true)
{
    protected override string Source { get; } = source;
}
