using AtlusScriptLibrary.Common.Libraries;
using AtlusScriptLibrary.Common.Text;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using AtlusScriptLibrary.MessageScriptLanguage.Decompiler;
using System.Text;
using Unreal.AtlusScript.Reloaded.AtlusScript.Types;
using Unreal.AtlusScript.Reloaded.Configuration;
using Unreal.ObjectsEmitter.Interfaces;
using Unreal.ObjectsEmitter.Interfaces.Types;

namespace Unreal.AtlusScript.Reloaded.AtlusScript;

internal unsafe class AtlusScriptService
{
	private readonly string dumpDir;
    private readonly MessageScriptCompiler compiler;
    private readonly Library gameLibrary;
    private DumpType dumpBmds;
    private DumpType dumpBfs;

    public AtlusScriptService(IUObjects uobjects, string modDir)
    {
        this.dumpDir = Directory.CreateDirectory(Path.Join(modDir, "dump")).FullName;
        uobjects.ObjectCreated += this.OnObjectCreated;

        AtlusEncoding.SetCharsetDirectory(Path.Join(modDir, "Charsets"));
        LibraryLookup.SetLibraryPath(Path.Join(modDir, "Libraries"));
        this.gameLibrary = LibraryLookup.GetLibrary("p3r");
        this.compiler = new MessageScriptCompiler(FormatVersion.Version1BigEndian, AtlusEncoding.Persona5RoyalEFIGS) { Library = this.gameLibrary };
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Needed for shift_jis encoding to be available
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
                Log.Information($"Dumped BMD: {obj.Name}");
            }
            else if (this.dumpBmds == DumpType.Decompile)
            {
                try
                {
                    this.DecompileBMD(obj);
                    Log.Information($"Decompiled and Dumped BMD: {obj.Name}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to decomile BMD: {obj.Name}");
                }
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

    private void DecompileBMD(UnrealObject obj)
    {
        var outputDir = Directory.CreateDirectory(Path.Join(this.dumpDir, obj.Name)).FullName;
        var outputFile = Path.Join(outputDir, $"{obj.Name}.msg");
        if (File.Exists(outputFile))
        {
            return;
        }

        var bmd = (UAtlusScriptAsset*)obj.Self;
        var span = new Span<byte>(bmd->mBuf.AllocatorInstance, bmd->mBuf.Num);
        var stream = new MemoryStream(span.ToArray());

        MessageScript script = MessageScript.FromStream(stream, FormatVersion.Version1Reload, Encoding.UTF8);
        using var decompiler = new MessageScriptDecompiler(new FileTextWriter(outputFile)) { Library = this.gameLibrary };
        decompiler.Decompile(script);
    }

    public void SetConfig(Config config)
	{
		this.dumpBmds = config.Dump_BMD;
        this.dumpBfs = config.Dump_BF;
	}
}
