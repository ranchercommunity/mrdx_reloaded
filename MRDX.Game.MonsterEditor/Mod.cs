﻿using Avalonia;
using MRDX.Game.MonsterEditor.Configuration;
using MRDX.Game.MonsterEditor.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

namespace MRDX.Game.MonsterEditor;

/// <summary>
///     Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    ///     Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    ///     Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    ///     The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    /// <summary>
    ///     Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    ///     Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    private readonly Task _uithread;

    /// <summary>
    ///     Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        _uithread = new Task(() =>
        {
            Program.BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(Array.Empty<string>());
        });
        _uithread.Start();
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    #endregion
}