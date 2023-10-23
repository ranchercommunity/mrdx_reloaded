using MRDX.Base.Mod.Interfaces;
using MRDX.Qol.BattleTimer.Configuration;
using MRDX.Qol.BattleTimer.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace MRDX.Qol.BattleTimer;

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

    private IHook<SetupCCtrlBattle>? _battleHook;

    /// <summary>
    ///     Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    private IHook<DecrementBattleTimer>? _timerHook;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        _modLoader.GetController<IHooks>().TryGetTarget(out var hooks);
        hooks!.AddHook<SetupCCtrlBattle>(SetupCCtrlBattleHook)
            .ContinueWith(result => _battleHook = result.Result.Activate());
        hooks!.AddHook<DecrementBattleTimer>(DecrementBattleTimerHook)
            .ContinueWith(result => _timerHook = result.Result.Activate());
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    private void SetupCCtrlBattleHook(nuint parent)
    {
        _battleHook!.OriginalFunction(parent);
        Memory.Instance.Read(nuint.Add(parent, 0x34), out short gameMode);
        var newTimer = gameMode switch
        {
            5 => _configuration.ErrantryBattleTimer * 30,
            6 => _configuration.DemoBattleTimer * 30,
            _ => _configuration.BattleTimer * 30
        };
        // The current timer that ticks down is in offset 0x10
        Memory.Instance.Write(nuint.Add(parent, 0x10), newTimer);
        // but the original timer value is in offset 0x14
        Memory.Instance.Write(nuint.Add(parent, 0x14), newTimer);
    }

    private void DecrementBattleTimerHook(nint self)
    {
        if (_configuration.FreezeTimer) return;

        _timerHook!.OriginalFunction(self);
    }


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