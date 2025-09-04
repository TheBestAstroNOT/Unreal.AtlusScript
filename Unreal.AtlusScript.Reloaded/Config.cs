using System.ComponentModel;
using Unreal.AtlusScript.Interfaces;
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

    [DisplayName("Decompile BF Endianess")]
    [Description("Set what endianess to use when decompiling BFs. Default is to try both if one fails, with BE first.")]
    [DefaultValue(Decomp_Endianess.Both)]
    public Decomp_Endianess Decomp_BF_Endian { get; set; } = Decomp_Endianess.Both;

    [DisplayName("Override Asset Locale")]
    [Description("Override the locale of assets to load.")]
    [DefaultValue(ESystemLanguage.Disabled)]
    public ESystemLanguage OverrideAssetLocale { get; set; } = ESystemLanguage.Disabled;
}

public enum DumpType
{
    Disabled,
    Binary_Data,
    Decompile,
}

public enum Decomp_Endianess
{
    Both,
    BIG_ENDIAN,
    LITTLE_ENDIAN,
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}