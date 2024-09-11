namespace Unreal.AtlusScript.Reloaded.AtlusScript;

internal class GameFunctions
{
    private delegate bool IsAstrea_();
    private IsAstrea_? isAstrea;

    public GameFunctions()
    {
        ScanHooks.Add(
            nameof(IsAstrea),
            "48 83 EC 28 E8 ?? ?? ?? ?? 48 85 C0 74 ?? E8 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 3C 01 0F 94 C0 48 83 C4 28 C3 48 83 C4 28 C3",
            (hooks, result) => this.isAstrea = hooks.CreateWrapper<IsAstrea_>(result, out _));
    }

    public bool IsAstrea() => this.isAstrea!();
}
