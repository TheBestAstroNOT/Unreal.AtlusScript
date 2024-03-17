using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Drawing;
using Unreal.AtlusScript.Reloaded.Configuration;
using Unreal.AtlusScript.Reloaded.Template;

namespace Unreal.AtlusScript.Reloaded;

public class Mod : ModBase
{
    public const string NAME = "Unreal.AtlusScript";

    private readonly IModLoader modLoader;
    private readonly IReloadedHooks? hooks;
    private readonly ILogger log;
    private readonly IMod owner;

    private Config config;
    private readonly IModConfig modConfig;

    public Mod(ModContext context)
    {
        this.modLoader = context.ModLoader;
        this.hooks = context.Hooks;
        this.log = context.Logger;
        this.owner = context.Owner;
        this.config = context.Configuration;
        this.modConfig = context.ModConfig;

#if DEBUG
        //Debugger.Launch();
#endif
        Log.Initialize(NAME, this.log, Color.White);
        Log.LogLevel = this.config.LogLevel;

        this.ApplyConfig();
    }

    private void ApplyConfig()
    {
        Log.LogLevel = this.config.LogLevel;
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
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}