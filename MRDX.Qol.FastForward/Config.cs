using System.ComponentModel;
using MRDX.Qol.FastForward.Template.Configuration;

namespace MRDX.Qol.FastForward;

public class Config : Configurable<Config>
{
    [DisplayName("Toggle FF Mode")]
    [Description("If ON pressing L will toggle fast forward. If OFF you need to hold L to fast forward.")]
    [DefaultValue(true)]
    public bool UseToggle { get; set; } = true;

    [DisplayName("Tick Delay")]
    [Description("The lower the value, the faster the game runs. Limitation: your monitor refresh rate will be the hard cap for how fast it can run.")]
    [DefaultValue(16000)]
    public int TickDelay { get; set; } = 16000;
}

/// <summary>
///     Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
///     Override elements in <see cref="ConfiguratorMixinBase" /> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}