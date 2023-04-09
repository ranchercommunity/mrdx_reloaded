using MRDX.Base.Mod.Interfaces;
using MRDX.Qol.FastForward.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

namespace MRDX.Qol.FastForward;

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

    private bool _wasPressed;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;

        var gameClient = _modLoader.GetController<IGameClient>();
        _modLoader.GetController<IController>().TryGetTarget(out var controller);
        controller!.OnInputChanged += input =>
        {
            var isPressed = (input.Buttons & ButtonFlags.LTrigger) != 0;
            if (_wasPressed != isPressed)
                if (gameClient != null && gameClient.TryGetTarget(out var game))
                    game.FastForwardOption = isPressed;
            _wasPressed = isPressed;
        };
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion
}