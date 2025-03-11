using MRDX.Base.Mod.Interfaces;
using MRDX.Qol.TurboInput.Configuration;
using MRDX.Qol.TurboInput.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;

namespace MRDX.Qol.TurboInput;

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

    private readonly WeakReference<IController> _input;

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

    private IDictionary<ButtonFlags, TurboState> _state = new Dictionary<ButtonFlags, TurboState>();

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        _input = _modLoader.GetController<IController>();
        _input.TryGetTarget(out var controller);

        if (controller == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Failed to grab controller input.");
            return;
        }

        // Setup to track specific buttons
        Reconfigure(null, _configuration);
        controller.SetInput += TurboInput;
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    private void TurboInput(ref IInput inputs)
    {
        foreach (var item in _state)
        {
            var button = item.Key;
            var state = item.Value;
            // If the user let go of the button, skip processing this state
            if ((inputs.Buttons & button) == 0)
            {
                state.IsEnabled = false;
                continue;
            }

            // otherwise they are holding the button. Reset if they just started holding the button
            if (!state.IsEnabled) state.Reset();

            state.Frames++;

            // Now check to see if any timers elapsed, if not go to the next one
            if (!state.IsElapsed()) continue;

            // Since the user is holding the button, we only need to turn it *off* while the turbo is off
            // we don't need to turn it on.
            if (!state.TurboOn)
                inputs.Buttons &= ~button;

            // Finally reset the frame and timer counts. If we are here, then we've at least finished the delay timer.
            state.Frames = 0;
            state.Start = DateTime.Now;
            state.DelayEnded = true;
            state.TurboOn = !state.TurboOn;
        }
    }

    // Add or remove the button from turbo tracking if the configuration just enabled it or just removed it
    private void EnableButtonTracking(bool oldVal, bool newVal, ButtonFlags flag)
    {
        if (!oldVal && newVal)
            _state.Add(flag, new TurboState(_configuration));
        else if (oldVal && !newVal)
            _state.Remove(ButtonFlags.Cross);
    }

    private void Reconfigure(Config? oldVal, Config newVal)
    {
        if (oldVal != null && (oldVal.Speed != newVal.Speed || oldVal.Delay != newVal.Delay))
        {
            // If the speed values changed, just wipe out all the state and start fresh
            _state = new Dictionary<ButtonFlags, TurboState>();
            Reconfigure(null, newVal);
        }

        EnableButtonTracking(oldVal?.TurboCircle ?? false, newVal.TurboCircle, ButtonFlags.Circle);
        EnableButtonTracking(oldVal?.TurboCross ?? false, newVal.TurboCross, ButtonFlags.Cross);
        EnableButtonTracking(oldVal?.TurboTriangle ?? false, newVal.TurboTriangle, ButtonFlags.Triangle);
        EnableButtonTracking(oldVal?.TurboSquare ?? false, newVal.TurboSquare, ButtonFlags.Square);
    }

    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        Reconfigure(_configuration, configuration);
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    #endregion

    private class TurboState
    {
        private readonly Config _config;

        public TurboState(Config config)
        {
            _config = config;
        }

        public DateTime Start { get; set; } = DateTime.Now;
        public int Frames { get; set; }

        public bool DelayEnded { get; set; }

        public bool TurboOn { get; set; }

        public bool IsEnabled { get; set; }

        public void Reset()
        {
            Frames = 0;
            Start = DateTime.Now;
            DelayEnded = false;
            TurboOn = false;
            IsEnabled = true;
        }

        public bool IsElapsed()
        {
            var delay = DelayEnded ? _config.Speed : _config.Delay;
            if (_config.UseFrameCount)
                return delay < Frames;

            var diff = DateTime.Now - Start;
            return delay < (int)diff.TotalMilliseconds;
        }
    }
}