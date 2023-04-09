using System.ComponentModel;
using MRDX.Qol.SkipDrillAnim.Template.Configuration;

namespace MRDX.Qol.SkipDrillAnim;

public class Config : Configurable<Config>
{
    [DisplayName("Auto Skip Drill")]
    [Description("When On, no input is required to skip the drill. When Off, you must press /\\ to skip")]
    [DefaultValue(true)]
    public bool AutoSkip { get; set; } = true;
}

/// <summary>
///     Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
///     Override elements in <see cref="ConfiguratorMixinBase" /> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}