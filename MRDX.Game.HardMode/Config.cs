using System.ComponentModel;
using MRDX.Game.HardMode.Template.Configuration;

namespace MRDX.Game.HardMode.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Use Original PS1 Lifespan")]
    [Description(
        "Sets the monster lifespan to the original values from the PS1 version (+100 weeks) instead of using the adjusted DX lifespan")]
    [DefaultValue(true)]
    public bool UseOriginalLifespan { get; set; } = false;
}

/// <summary>
///     Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
///     Override elements in <see cref="ConfiguratorMixinBase" /> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}