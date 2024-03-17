using Reloaded.Mod.Interfaces.Structs;
using System.ComponentModel;
using Unreal.AtlusScript.Reloaded.Template.Configuration;

namespace Unreal.AtlusScript.Reloaded.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Log Level")]
    [DefaultValue(LogLevel.Information)]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    [DisplayName("Dump BMDs")]
    [Description("Dumps BMD objects to mod folder: Unreal.AtlusScript/dump/*")]
    [DefaultValue(DumpType.Disabled)]
    public DumpType Dump_BMD { get; set; } = DumpType.Disabled;

    [DisplayName("Dump BFs")]
    [Description("Dumps BF objects to mod folder: Unreal.AtlusScript/dump/*")]
    [DefaultValue(DumpType.Disabled)]
    public DumpType Dump_BF { get; set; } = DumpType.Disabled;
}

public enum DumpType
{
    Disabled,
    Binary_Data,
    Decompile,
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}