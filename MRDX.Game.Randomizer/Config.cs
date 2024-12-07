using System.ComponentModel;
using MRDX.Game.Randomizer.Template.Configuration;

namespace MRDX.Game.Randomizer.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Flag String")]
    [Description("Current seed, flags, and settings used for this game")]
    [DefaultValue("")]
    public string FlagString { get; set; } = "";
}

/// <summary>
///     Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
///     Override elements in <see cref="ConfiguratorMixinBase" /> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}