using System.ComponentModel;
using MRDX.Qol.FastForward.Template.Configuration;

namespace MRDX.Qol.FastForward;

public class Config : Configurable<Config>
{
    [DisplayName("Toggle FF Mode")]
    [Description("If ON pressing L will toggle fast forward. If OFF you need to hold L to fast forward.")]
    [DefaultValue(true)]
    public bool UseToggle { get; set; } = true;

    [DisplayName("Next Frame Delay (microseconds)")]
    [Description("The lower the value, the faster the game runs.\n" +
                 "This is the number of microseconds the game waits before starting the next frame.\n" +
                 "Limitation: your monitor refresh rate will be the hard cap for how fast it can run.")]
    [DefaultValue(16000)]
    public int TickDelay { get; set; } = 16000;

    [DisplayName("Disable VSync")]
    [Description(
        "When ON, makes the fast forward go even faster at the cost of causing tearing and other visual glitches")]
    [DefaultValue(false)]
    public bool DisableVsync { get; set; } = false;
}

/// <summary>
///     Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
///     Override elements in <see cref="ConfiguratorMixinBase" /> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}