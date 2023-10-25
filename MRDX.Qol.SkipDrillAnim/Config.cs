using System.ComponentModel;
using MRDX.Qol.SkipDrillAnim.Template.Configuration;

namespace MRDX.Qol.SkipDrillAnim;

public class Config : Configurable<Config>
{
    public enum SkipAnimationSetting
    {
        Auto,
        Manual,
        Disabled
    }

    [DisplayName("Skip Drill Animations")]
    [Description(
        "Auto - Skips the drill instantly. Manual - Press Triangle /\\ to skip. Disabled - Do not allow skipping drill animations")]
    [DefaultValue(SkipAnimationSetting.Auto)]
    public SkipAnimationSetting SkipDrill { get; set; } = SkipAnimationSetting.Auto;

    [DisplayName("Skip Expedition Item Find Animations")]
    [Description("On - Skips the item maze animation instantly. Off - Do not skip item find animations")]
    [DefaultValue(true)]
    public bool SkipItemFind { get; set; } = true;
}

/// <summary>
///     Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
///     Override elements in <see cref="ConfiguratorMixinBase" /> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}