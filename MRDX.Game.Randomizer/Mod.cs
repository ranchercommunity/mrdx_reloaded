using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Game.Randomizer.Configuration;
using MRDX.Game.Randomizer.Template;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Universal.Redirector.Interfaces;

namespace MRDX.Game.Randomizer;

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

        _logger.WriteLine(
            $"[MRDX Randomizer] Default flagstring {new RandomizerConfig().ToFlags()}");

        _modLoader.GetController<IRedirectorController>().TryGetTarget(out var redirector);
        if (redirector == null)
        {
            _logger.WriteLine(
                "[MRDX Randomizer] Could not initialize randomizer, failed to get redirector!");
            return;
        }

        var randomizer = Randomizer.Create(_logger, redirector, _modLoader.GetDirectoryForModId("MRDX.Game.Randomizer"),
            _configuration.FlagString);
        if (randomizer == null)
        {
            _logger.WriteLine(
                "[MRDX Randomizer] Could not initialize randomizer, failed to launch randomizer!");
            return;
        }

        if (!_modLoader.GetController<IExtractDataBin>().TryGetTarget(out var extract))
        {
            _logger.WriteLine(
                "[MRDX Randomizer] Could not initialize extraction, failed to extract bin!");
            return;
        }

        if (extract.ExtractedPath != null)
            randomizer.DataExtractComplete(extract.ExtractedPath);
        else
            extract.ExtractComplete += randomizer.DataExtractComplete;
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
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    #endregion
}