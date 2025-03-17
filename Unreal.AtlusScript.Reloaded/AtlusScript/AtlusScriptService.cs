using AtlusScriptLibrary.Common.Libraries;
using AtlusScriptLibrary.Common.Text;
using AtlusScriptLibrary.FlowScriptLanguage;
using AtlusScriptLibrary.FlowScriptLanguage.Decompiler;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Decompiler;
using System.Runtime.InteropServices;
using System.Text;
using Unreal.AtlusScript.Interfaces;
using Unreal.AtlusScript.Reloaded.AtlusScript.Types;
using Unreal.AtlusScript.Reloaded.Configuration;
using Unreal.ObjectsEmitter.Interfaces;
using Unreal.ObjectsEmitter.Interfaces.Types;
using static Unreal.AtlusScript.Reloaded.AtlusScript.AtlusAssetsRegistry;

namespace Unreal.AtlusScript.Reloaded.AtlusScript;

internal unsafe class AtlusScriptService
{
    private delegate ESystemLanguage GetLanguage();
    private readonly SHFunction<GetLanguage>? _GetLanguage;

    private readonly IUnreal unreal;
    private readonly AtlusAssetsRegistry assetsRegistry;
    private readonly FlowScriptDecompiler flowDecompiler;
    private readonly Library gameLibrary;
    private readonly GameFunctions game;
    private readonly string dumpDir;

    private Config configuration;
    private DumpType dumpBmds;
    private DumpType dumpBfs;
    private Decomp_Endianess decompBfEndian;

    public AtlusScriptService(
        IUObjects uobjects,
        IUnreal unreal,
        AtlusAssetsRegistry registry,
        FlowScriptDecompiler flowDecompiler,
        Library gameLibrary,
        string modDir,
        Config config)
    {
        this.unreal = unreal;
        this.assetsRegistry = registry;
        this.flowDecompiler = flowDecompiler;
        this.gameLibrary = gameLibrary;
        this.game = new();
        this.configuration = config;
        this.dumpDir = Directory.CreateDirectory(Path.Join(modDir, "dump")).FullName;
        uobjects.ObjectCreated += this.OnObjectCreated;
        _GetLanguage = new SHFunction<GetLanguage>("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 30 E8");        
    }

    private void OnObjectCreated(UnrealObject obj)
    {
        var isBmdObj = obj.Name.StartsWith("bmd", StringComparison.OrdinalIgnoreCase);
        var isBfObj = obj.Name.StartsWith("bf", StringComparison.OrdinalIgnoreCase);
        if (isBmdObj == false && isBfObj == false)
        {
            return;
        }

        if (isBmdObj)
        {
            Log.Debug($"Loaded: {obj.Name}");

            var bmd = (UAtlusScriptAsset*)obj.Self;
            if (this.dumpBmds == DumpType.Binary_Data)
            {
                var outputFile = Path.Join(this.dumpDir, $"{obj.Name}.bmd");
                DumpBinaryData(bmd->mBuf, outputFile);
            }
            else if (this.dumpBmds == DumpType.Decompile)
            {
                try
                {
                    this.DecompileBMD(obj);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to decomile BMD: {obj.Name}");
                }
            }
        }
        else
        {
            Log.Debug($"Loaded: {obj.Name}");

            var bf = (UAtlusScriptAsset*)obj.Self;
            if (this.dumpBfs == DumpType.Binary_Data)
            {
                var outputFile = Path.Join(this.dumpDir, $"{obj.Name}.bf");
                DumpBinaryData(bf->mBuf, outputFile);
            }
            else if (this.dumpBfs == DumpType.Decompile)
            {
                try
                {
                    var outputFile = Path.Join(this.dumpDir, $"{obj.Name}.flow");
                    this.DecompileBF(obj, outputFile);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to decompile BF: {obj.Name}");
                }
            }
        }

        var mode = this.game.IsAstrea() ? AssetMode.Astrea : AssetMode.Default;
        ESystemLanguage currentLang = ESystemLanguage.EN;
        if (configuration.Override_Asset_Locale == AssetConfigLanguage.Disabled)
        {
            currentLang = _GetLanguage.OriginalFunction();
        }
        else
        {
            currentLang = (ESystemLanguage)configuration.Override_Asset_Locale;
        }

        if (this.assetsRegistry.TryGetAsset(mode, obj.Name, out var data, currentLang))
        {
            var objAsset = (UAtlusScriptAsset*)obj.Self;

            var buffer = this.unreal.FMalloc(data.Length, 0);
            Marshal.Copy(data, 0, buffer, data.Length);

            objAsset->mBuf.Num = data.Length;
            objAsset->mBuf.Max = data.Length;
            objAsset->mBuf.AllocatorInstance = (byte*)buffer;
            Log.Debug($"Custom Asset ({mode}): {obj.Name}");
        }
        ;
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

        Log.Information($"Dumped file: {Path.GetFileName(outputFile)}");
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
        using var stream = new MemoryStream(span.ToArray());
        var script = MessageScript.FromStream(stream, AtlusScriptLibrary.MessageScriptLanguage.FormatVersion.Version1Reload, Encoding.UTF8);
        using var decompiler = new MessageScriptDecompiler(new FileTextWriter(outputFile)) { Library = this.gameLibrary };
        decompiler.Decompile(script);

        Log.Information($"Decompiled: {obj.Name}");
    }

    private void DecompileBF(UnrealObject obj, string outputFile)
    {
        if (File.Exists(outputFile))
        {
            return;
        }

        var bf = (UAtlusScriptAsset*)obj.Self;
        var bfData = new Span<byte>(bf->mBuf.AllocatorInstance, bf->mBuf.Num).ToArray();

        if (this.decompBfEndian == Decomp_Endianess.Both || this.decompBfEndian == Decomp_Endianess.BIG_ENDIAN)
        {
            try
            {
                var flowBE = FlowScript.FromStream(new MemoryStream(bfData), Encoding.UTF8, AtlusScriptLibrary.FlowScriptLanguage.FormatVersion.Version4BigEndian, false);

                // Decompile successfully with BE.
                if (this.flowDecompiler.TryDecompile(flowBE, outputFile))
                {
                    Log.Information($"Decompiled with BE: {Path.GetFileName(outputFile)}");
                    return;
                }

                // Decompile failed with BE but not set to try both, so exit.
                if (decompBfEndian == Decomp_Endianess.BIG_ENDIAN)
                {
                    Log.Error($"Failed to decompile BF with BE: {obj.Name}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to decompile BF with BE: {obj.Name}");
                Log.Information("Trying to decompile with LE...");
            }
        }

        // Decompile with LE.
        try
        {
            var flowLE = FlowScript.FromStream(new MemoryStream(bfData), Encoding.UTF8, AtlusScriptLibrary.FlowScriptLanguage.FormatVersion.Version4, false);

            // Decompile successfully with BE.
            if (this.flowDecompiler.TryDecompile(flowLE, outputFile))
            {
                Log.Information($"Decompiled with LE: {Path.GetFileName(outputFile)}");
            }
            else
            {
                Log.Information($"Failed to decompile with LE: {Path.GetFileName(outputFile)}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to decompile BF with LE: {obj.Name}");
        }
    }

    public void SetConfig(Config config)
	{
		this.dumpBmds = config.Dump_BMD;
        this.dumpBfs = config.Dump_BF;
        this.decompBfEndian = config.Decomp_BF_Endian;
	}
}
