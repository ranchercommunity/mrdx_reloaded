using MRDX.Base.Mod.Interfaces;
using MRDX.Qol.FastForward.Template;

namespace MRDX.Qol.FastForward;

/// <summary>
///     Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    private readonly WeakReference<IController>? _controller;
    private readonly WeakReference<IGameClient>? _gameClient;

    /// <summary>
    ///     Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    private bool _wasPressed;

    public Mod(ModContext context)
    {
        _configuration = context.Configuration;

        _gameClient = context.ModLoader.GetController<IGameClient>();
        _controller = context.ModLoader.GetController<IController>();
        _controller.TryGetTarget(out var controller);
        if (controller == null)
        {
            Logger.Error("Could not get controller controller.");
            return;
        }

        if (_gameClient == null || !_gameClient.TryGetTarget(out var game))
        {
            Logger.Error("Could not get game client controller.");
            return;
        }

        game.TickDelay = _configuration.TickDelay;
        game.SetVsyncEnable(!_configuration.DisableVsync);

        if (game.FastForwardOption)
            game.SetFastForward(true);

        controller.OnInputChanged += HandleInputChanged;
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion


    private void HandleInputChanged(IInput input)
    {
        if (_gameClient == null || !_gameClient.TryGetTarget(out var game)) return;

        var originalff = game.FastForwardOption;
        var ff = originalff;
        var isPressed = input.Buttons.HasFlag(ButtonFlags.LTrigger);
        if (_configuration.UseToggle)
        {
            // If the user just pressed the toggle button, then change the fast forward state.
            if (isPressed && !_wasPressed)
                ff = !ff;
        }
        else
        {
            if (_wasPressed != isPressed)
                ff = isPressed;
        }

        if (originalff != ff)
            game.SetFastForward(ff);


        _wasPressed = isPressed;
    }

    public override void ConfigurationUpdated(Config configuration)
    {
        _configuration = configuration;
        if (_controller?.TryGetTarget(out var controller) ?? false)
            HandleInputChanged(controller.Current);
        if (_gameClient == null || !_gameClient.TryGetTarget(out var game)) return;

        game.TickDelay = _configuration.TickDelay;
        game.SetVsyncEnable(!_configuration.DisableVsync);

        Logger.Info("Config Updated: Applying");
    }
}