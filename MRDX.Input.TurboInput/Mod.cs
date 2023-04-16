using MRDX.Base.Mod.Interfaces;
using MRDX.Input.TurboInput.Configuration;
using MRDX.Input.TurboInput.Template;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

namespace MRDX.Input.TurboInput
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase // <= Do not Remove.
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly IReloadedHooks? _hooks;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        private readonly WeakReference<IController> _input;

        private readonly IController _controller;

        private DateTime _start = DateTime.Now;
        private int _tick = 0;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;


            // For more information about this template, please see
            // https://reloaded-project.github.io/Reloaded-II/ModTemplate/

            // If you want to implement e.g. unload support in your mod,
            // and some other neat features, override the methods in ModBase.

            _input = _modLoader.GetController<IController>();
            _input.TryGetTarget(out var controller);

            if (controller == null)
            {
                _logger.WriteLine($"[{_modConfig.ModId}] Failed to grab controller input.");
                return;
            }

            controller.SetInput += TurboInput;
        }

        private void TurboInput(ref IInput inputs)
        {
            bool turbo = true;

            _tick++;

            if (_configuration.Delay > 0)
            {
                int elapsed = GetElapsed();

                turbo = elapsed >= _configuration.Delay;
            }

            if (turbo)
            {
                inputs.Buttons &= ~(ButtonFlags.Triangle | ButtonFlags.Cross);
                _start = DateTime.Now;
                _tick = 0;
            }
        }

        private int GetElapsed()
        {
            if (_configuration.UseTick)
                return _tick;

            TimeSpan diff = DateTime.Now - _start;
            return (int) diff.TotalMilliseconds;
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

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}