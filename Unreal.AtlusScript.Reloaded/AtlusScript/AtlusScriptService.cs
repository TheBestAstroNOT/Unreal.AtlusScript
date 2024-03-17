using Unreal.AtlusScript.Reloaded.AtlusScript.Types;
using Unreal.AtlusScript.Reloaded.Configuration;
using Unreal.ObjectsEmitter.Interfaces;
using Unreal.ObjectsEmitter.Interfaces.Types;

namespace Unreal.AtlusScript.Reloaded.AtlusScript;

internal unsafe class AtlusScriptService
{
	private readonly string dumpDir;
    private DumpType dumpBmds;
    private DumpType dumpBfs;

    public AtlusScriptService(IUObjects uobjects, string dumpDir)
    {
        this.dumpDir = dumpDir;
        uobjects.ObjectCreated += this.OnObjectCreated;
    }

    private void OnObjectCreated(UnrealObject obj)
    {
        if (obj.Name.StartsWith("bmd", StringComparison.OrdinalIgnoreCase))
        {
			Log.Debug(obj.Name);

            var bmd = (UAtlusScriptAsset*)obj.Self;
            if (this.dumpBmds == DumpType.Binary_Data)
            {
                var outputFile = Path.Join(this.dumpDir, $"{obj.Name}.bmd");
                DumpBinaryData(bmd->mBuf, outputFile);
                Log.Information($"Dumped BMD: {Path.GetFileName(outputFile)}");
            }
            else if (this.dumpBmds == DumpType.Decompile)
            {

            }
        }

        if (obj.Name.StartsWith("bf", StringComparison.OrdinalIgnoreCase))
        {
            Log.Debug(obj.Name);

            var bf = (UAtlusScriptAsset*)obj.Self;
            if (this.dumpBfs == DumpType.Binary_Data)
            {
                var outputFile = Path.Join(this.dumpDir, $"{obj.Name}.bf");
                DumpBinaryData(bf->mBuf, outputFile);
                Log.Information($"Dumped BF: {Path.GetFileName(outputFile)}");
            }
            else if (this.dumpBmds == DumpType.Decompile)
            {

            }
        }
    }

    private static void DumpBinaryData(TArray<byte> data, string outputFile)
    {
        if (File.Exists(outputFile))
        {
            return;
        }

        using var fs = new FileStream(outputFile, FileMode.Create);
        var span = new Span<byte>(data.AllocatorInstance, data.Num);
        fs.Write(span);
    }

    public void SetConfig(Config config)
	{
		this.dumpBmds = config.Dump_BMD;
        this.dumpBfs = config.Dump_BF;
	}
}
