using System.ComponentModel;
using MRDX.Qol.BattleTimer.Template.Configuration;

namespace MRDX.Qol.BattleTimer.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Freeze Timer")]
    [Description("Prevent the battle timer from ticking down.\nYou can change this at any time even during gameplay.")]
    [DefaultValue(true)]
    public bool FreezeTimer { get; set; } = true;

    [DisplayName("Battle Timer")]
    [Description("Initial Value for the battle timer (in seconds)")]
    [DefaultValue(60)]
    public int BattleTimer { get; set; } = 60;

    [DisplayName("Errantry Battle Timer")]
    [Description("Initial Value for the battle timer when fighting an errantry battl (in seconds)")]
    [DefaultValue(180)]
    public int ErrantryBattleTimer { get; set; } = 180;

    [DisplayName("Demo Battle Timer")]
    [Description("Initial Value for the battle timer for the Demo Fight in the opening sequence (in seconds)")]
    [DefaultValue(30)]
    public int DemoBattleTimer { get; set; } = 30;
}

/// <summary>
///     Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
///     Override elements in <see cref="ConfiguratorMixinBase" /> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}