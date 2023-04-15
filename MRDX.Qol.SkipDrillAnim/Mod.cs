using MRDX.Base.Mod.Interfaces;
using MRDX.Qol.SkipDrillAnim.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Diagnostics;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sources;

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

    [Function(CallingConventions.Cdecl)]
    private delegate bool IsTrainingWrapper(IntPtr unk1, int unk2, IntPtr unk3);

    private IHook<IsTrainingWrapper>? _wrapperHook;

    private bool _isAtErrantryBattleEnd = false;

    // No idea on how to get this value at run time through sig scanning, but this is the vftable for CModePast
    private const int _cModePastAddr = 0x00D167D4;

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

        var startupScanner = _modLoader.GetController<IStartupScanner>();
        startupScanner.TryGetTarget(out var scanner);
        if (scanner != null)
        {
            var thisProcess = Process.GetCurrentProcess();
            var baseAddress = thisProcess.MainModule.BaseAddress;
            scanner.AddMainModuleScan(
                "55 8B EC 8B 45 10 8B 48 2C",
                result => _wrapperHook = _hooks.CreateHook<IsTrainingWrapper>(Wrapper, baseAddress + result.Offset).Activate()
            );
        }
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
        if (_isAtErrantryBattleEnd == true)
        {
            _isAtErrantryBattleEnd = false;
            return _hook.OriginalFunction(self);
        }

        if (_isAutoSkipEnabled)
            return true;
        if (!_input.TryGetTarget(out var controller))
            return false;
        return (controller.Current.Buttons & (ButtonFlags.Circle | ButtonFlags.Triangle)) != 0;
    }

    /**
     * From what I can understand there are two cases where this function gets called:
     * 1. During an ongoing drill
     * 2. During the cutscene after an errantry battle
     *
     * There may be other cases where this does get called, and we might have to handle it
     * a similar way we're handling #2.
     */
    private bool Wrapper(IntPtr unk1, int unk2, IntPtr unk3)
    {
        Memory.Instance.Read<IntPtr>((nuint) unk1, out var unk1Val);

        if (unk1Val == _cModePastAddr)
        {
            // Becomes relevant in ShouldSkipTraining
            _isAtErrantryBattleEnd = true;
        }

        return _wrapperHook.OriginalFunction(unk1, unk2, unk3);
    }
}