using System.ComponentModel;
using MRDX.Audio.VolumeConfig.Template.Configuration;

namespace MRDX.Audio.VolumeConfig;

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

    [DisplayName("Sound Effects Volume")]
    [Description(
        "Controls the volume for menu sounds and other non-game sound effects.\nRange: 0-100 where 0 means mute")]
    [DefaultValue(100)]
    public int SfxVolume { get; set; } = 100;

    [DisplayName("Background Music Volume")]
    [Description("Controls the volume for in game background music.\nRange: 0-100 where 0 means mute")]
    [DefaultValue(100)]
    public int MusicVolume { get; set; } = 100;

    [DisplayName("Movie Volume")]
    [Description(
        "Controls the volume for prerendered cutscenes, such as the opening movie.\nRange: 0-100 where 0 means mute\nONLY FOR MR2DX")]
    [DefaultValue(100)]
    public int FmvVolume { get; set; } = 100;
}

/// <summary>
///     Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
///     Override elements in <see cref="ConfiguratorMixinBase" /> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}