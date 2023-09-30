using MRDX.Base.Mod.Interfaces;
using MRDX.Qol.SkipDrillAnim.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory;
using Reloaded.Mod.Interfaces;

namespace MRDX.Qol.SkipDrillAnim;

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

    private IHook<IsTrainingDone>? _hook;

    private bool _isAutoSkipEnabled;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;
        _isAutoSkipEnabled = context.Configuration.AutoSkip;

        _modLoader.GetController<IHooks>().TryGetTarget(out var hooks);
        hooks!.AddHook<IsTrainingDone>(ShouldSkipTraining).ContinueWith(result => _hook = result.Result?.Activate());
        _input = _modLoader.GetController<IController>();
    }


    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    public override void ConfigurationUpdated(Config configuration)
    {
        _isAutoSkipEnabled = configuration.AutoSkip;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    private bool ShouldSkipTraining(nint self)
    {
        _input.TryGetTarget(out var controller);
        if (_isAutoSkipEnabled || (controller?.Current.Buttons & (ButtonFlags.Circle | ButtonFlags.Triangle)) != 0)
            // The number at offset 8 is checked by the original function to see if we are done, and so we should update it before
            // returning from this function if we ended. In my testing the game usually sets it to 2 after the training animation ends.
            Memory.Instance.Write(nuint.Add((nuint)self, 8), 2);
        return _hook!.OriginalFunction(self);
    }
}