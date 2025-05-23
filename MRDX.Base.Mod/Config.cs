﻿using System.ComponentModel;
using MRDX.Base.Mod.Interfaces;
using MRDX.Base.Mod.Template.Configuration;

namespace MRDX.Base.Mod;

public class Config : Configurable<Config>
{
    [Category("Bug Fixes")]
    [DisplayName("Fix Monster Breed Pluralization")]
    [Description(
        "[MR2] Fixes the names of the monster breed in the viewer to no longer be plural")]
    [DefaultValue(true)]
    public bool FixMonsterBreedPluralization { get; set; } = true;

    [Category("Advanced - Mod Debugging")]
    [DisplayName("Reloaded Message Verbosity")]
    [Description(
        """
        Enables internal printouts to the Reloaded Log file to help debug issues or track mod performance.
        Error - No debug messages printed except dire, urgent issues. For normal gameplay.
        Warning - Prints messages for major events only.
        Info - Prints lots messages. Useful if there is consistent crashing.
        Debug - Prints so many that the the log may be a source of issue itself. Most helpful for diagnoisng issues though.
        Trace - Meant for diagnosing issues internal to the mod's performance itself.
        """)]
    [DefaultValue(Logger.LogLevel.Error)]
    public Logger.LogLevel LogLevel { get; set; } = Logger.LogLevel.Error;

    [Category("Advanced - Mod Debugging")]
    [DisplayName("Enable Debugging")]
    [Description(
        """
        DO NOT ENABLE UNLESS YOU KNOW WHAT YOU ARE DOING.
        On launch, will enable the Just In Time debugger. This is most useful when you build locally with debug symbols
        """)]
    [DefaultValue(false)]
    public bool EnableDebugging { get; set; } = false;
}

/// <summary>
///     Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
///     Override elements in <see cref="ConfiguratorMixinBase" /> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}