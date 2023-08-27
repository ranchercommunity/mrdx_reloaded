using System.ComponentModel;
using MRDX.Base.Mod.Template.Configuration;

namespace MRDX.Base.Mod;

public class Config : Configurable<Config>
{
    [DisplayName("Fix Monster Breed Pluralization")]
    [Description(
        "[MR2] Fixes the names of the monster breed in the viewer to no longer be plural")]
    [DefaultValue(true)]
    public bool FixMonsterBreedPluralization { get; set; } = true;
}

/// <summary>
///     Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
///     Override elements in <see cref="ConfiguratorMixinBase" /> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}