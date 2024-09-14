using System.Runtime.InteropServices;

namespace Unreal.AtlusScript.Reloaded.AtlusScript;

internal unsafe class GameFunctions
{
    private delegate UGlobalWork* GetGlobalWork();
    private GetGlobalWork? getGlobalWork;

    public GameFunctions()
    {
        ScanHooks.Add(
            nameof(GetGlobalWork),
            "48 89 5C 24 ?? 57 48 83 EC 20 48 8B 0D ?? ?? ?? ?? 33 DB",
            (hooks, result) => this.getGlobalWork = hooks.CreateWrapper<GetGlobalWork>(result, out _));
    }

    public bool IsPlayingAstrea()
    {
        var gwork = this.getGlobalWork!();
        Log.Information($"Playing Astrea: {gwork->isPlayingAstrea}\nAstrea Save: {gwork->isAstreaSave}");
        return this.getGlobalWork!()->isPlayingAstrea;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct UGlobalWork
    {
        [FieldOffset(0x30A30)]
        public bool isPlayingAstrea;

        [FieldOffset(0x30A40)]
        public bool isAstreaSave;
    }
}
