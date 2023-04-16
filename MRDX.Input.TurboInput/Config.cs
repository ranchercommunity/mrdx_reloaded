using MRDX.Input.TurboInput.Template.Configuration;
using System.ComponentModel;

namespace MRDX.Input.TurboInput.Configuration
{
    public class Config : Configurable<Config>
    {
        [DisplayName("Delay (ms)")]
        [Description("Turbo delay.")]
        [DefaultValue(0)]
        public int Delay { get; set; } = 50;

        [DisplayName("Use tick delay")]
        [Description("Use tick delay instead of ms.")]
        [DefaultValue(false)]
        public bool UseTick { get; set; } = false;
    }

    /// <summary>
    /// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
    /// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
    /// </summary>
    public class ConfiguratorMixin : ConfiguratorMixinBase
    {
        // 
    }
}