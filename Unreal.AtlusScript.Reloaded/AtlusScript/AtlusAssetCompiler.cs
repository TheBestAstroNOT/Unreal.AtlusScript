using AtlusScriptLibrary.FlowScriptLanguage.Compiler;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;

namespace Unreal.AtlusScript.Reloaded.AtlusScript;

internal class AtlusAssetCompiler(FlowScriptCompiler flowCompiler, MessageScriptCompiler msgCompiler)
{
    private readonly FlowScriptCompiler flowCompiler = flowCompiler;
    private readonly MessageScriptCompiler msgCompiler = msgCompiler;

    public byte[]? CompileBMD(string assetName, string msgContent)
    {
        if (msgCompiler.TryCompile(msgContent, out var script))
        {
            using var ms = new MemoryStream();
            script.ToStream(ms);

            return ms.ToArray();
        }
        else
        {
            Log.Error($"Failed to compile message: {assetName}");
            return null;
        }
    }

    public byte[]? CompileBF(string assetName, string flowContent)
    {
        if (this.flowCompiler.TryCompile(flowContent, out var flow))
        {
            using var ms = new MemoryStream();
            flow.ToStream(ms);

            return ms.ToArray();
        }
        else
        {
            Log.Error($"Failed to compile flow: {assetName}");
            return null;
        }
    }
}
