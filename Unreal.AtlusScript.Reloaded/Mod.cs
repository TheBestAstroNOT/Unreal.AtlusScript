using AtlusScriptLibrary.Common.Libraries;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.FlowScriptLanguage.Compiler;
using AtlusScriptLibrary.FlowScriptLanguage.Decompiler;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using Unreal.AtlusScript.Interfaces;
using Unreal.AtlusScript.Reloaded.AtlusScript;
using Unreal.AtlusScript.Reloaded.Configuration;
using Unreal.AtlusScript.Reloaded.Template;
using Unreal.ObjectsEmitter.Interfaces;

namespace Unreal.AtlusScript.Reloaded;

public class Mod : ModBase, IExports
{
    public const string NAME = "Unreal.AtlusScript";

    private readonly IModLoader modLoader;
    private readonly IReloadedHooks? hooks;
    private readonly ILogger log;
    private readonly IMod owner;

    private Config config;
    private readonly IModConfig modConfig;

    private readonly AtlusScriptService atlusScript;
    private readonly AtlusAssetsRegistry atlusRegistry;

    public Mod(ModContext context)
    {
        this.modLoader = context.ModLoader;
        this.hooks = context.Hooks;
        this.log = context.Logger;
        this.owner = context.Owner;
        this.config = context.Configuration;
        this.modConfig = context.ModConfig;

#if DEBUG
        Debugger.Launch();
#endif

        Project.Init(this.modConfig, this.modLoader, this.log, true);
        Log.LogLevel = this.config.LogLevel;

        var modDir = this.modLoader.GetDirectoryForModId(this.modConfig.ModId);
        this.modLoader.GetController<IUObjects>().TryGetTarget(out var uobjects);
        this.modLoader.GetController<IUnreal>().TryGetTarget(out var unreal);
        this.modLoader.GetController<IStartupScanner>().TryGetTarget(out var scanner);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Needed for shift_jis encoding to be available
        AtlusEncoding.SetCharsetDirectory(Path.Join(modDir, "Charsets"));
        LibraryLookup.SetLibraryPath(Path.Join(modDir, "Libraries"));
        var gameLibrary = LibraryLookup.GetLibrary("p3re");

        var msgCompiler = new MessageScriptCompiler(AtlusScriptLibrary.MessageScriptLanguage.FormatVersion.Version1BigEndian, Encoding.UTF8) { Library = gameLibrary };
        var flowDecompiler = new FlowScriptDecompiler() { Library = gameLibrary, SumBits = true };
        var flowCompiler = new FlowScriptCompiler(AtlusScriptLibrary.FlowScriptLanguage.FormatVersion.Version4BigEndian) { Encoding = Encoding.UTF8, Library = gameLibrary };

        var assetCompiler = new AtlusAssetCompiler(flowCompiler, msgCompiler);
        this.atlusRegistry = new(assetCompiler);
        this.atlusScript = new(uobjects!, unreal!, this.atlusRegistry, flowDecompiler, gameLibrary, modDir);

        this.modLoader.AddOrReplaceController<IAtlusAssets>(this.owner, this.atlusRegistry);
        this.modLoader.ModLoading += this.OnModLoading;
        this.ApplyConfig();

        Project.Start();
    }

    private void OnModLoading(IModV1 mod, IModConfigV1 config)
    {
        if (!config.ModDependencies.Contains(this.modConfig.ModId))
        {
            return;
        }

        var modDir = this.modLoader.GetDirectoryForModId(config.ModId);
        this.atlusRegistry.RegisterMod(new(config.ModId, modDir));
    }

    private void ApplyConfig()
    {
        Log.LogLevel = this.config.LogLevel;
        this.atlusScript.SetConfig(this.config);
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        config = configuration;
        log.WriteLine($"[{modConfig.ModId}] Config Updated: Applying");
        this.ApplyConfig();
    }

    public Type[] GetTypes() => new Type[] { typeof(IAtlusAssets) };
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}