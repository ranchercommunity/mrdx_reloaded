using System.ComponentModel;
using MRDX.Game.Randomizer.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;

namespace MRDX.Game.Randomizer.Configuration;

public class Config : Configurable<Config>
{
    /*
        User Properties:
            - Please put all of your configurable properties here.

        By default, configuration saves as "Config.json" in mod user config folder.
        Need more config files/classes? See Configuration.cs

        Available Attributes:
        - Category
        - DisplayName
        - Description
        - DefaultValue

        // Technically Supported but not Useful
        - Browsable
        - Localizable

        The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
    */

    [DisplayName("Flag String")]
    [Description("Current seed, flags, and settings used for this game")]
    [DefaultValue("")]
    public string FlagString { get; set; } = "";
}

public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}