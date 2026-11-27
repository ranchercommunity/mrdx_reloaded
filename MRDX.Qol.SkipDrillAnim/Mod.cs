using System.Drawing;
using MRDX.Base.Mod.Interfaces;
using MRDX.Qol.SkipDrillAnim.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using CallingConventions = Reloaded.Hooks.Definitions.X86.CallingConventions;

namespace MRDX.Qol.SkipDrillAnim;

/// <summary>
///     Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    // Instead of setting the trigger to be O or the △ button, use the back button (0x40) as the trigger.
    private const byte backButton = 0x40;

    private const string drillSignature = "66 8B 45 14 66 89 43 7A 66 8B 45 10 C7 03 ?? ?? ?? ?? C7 43 34 ?? ?? ?? ?? 66 89 43 7C 8B 06 FF 50 50";
    private const string buttonPress = "83 BE 48 01 00 00 5A 8B 7F 2C 7E 10 F6 05 ?? ?? ?? ?? 40 74 07 8B CF E8 ?? ?? ?? ?? 83 7F 7C 00";

    // Offset used to read the drill vtable and pressed buttons from memory
    private const int pointerOffset = 14;

    [HookDef(BaseGame.Mr2, Region.Us, "55 8B EC 8B 45 10 8B 48 2C 83 79 7C 00 75 10 8B 01 FF 50 5C")]
    [Function(CallingConventions.Cdecl)]
    public delegate int AnimationStart(nint mode, nint child, nint context);

    [HookDef(BaseGame.Mr2, Region.Us, "56 8B F1 C7 46 7C 01 00 00 00 C7 46 74 01 00 00 00")]
    [Function(CallingConventions.MicrosoftThiscall)]
    public delegate int AnimationFinish(nint animation);
    
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

    private IHook<AnimationStart>? _drillAnimationHook;
    private AnimationFinish? _AnimationFinish;
    private nuint _drillMode;
    private nuint _pressedButtons;
    
    private CheckIfMonsterGrabbedAnItem? _itemfunc;
    private IHook<InitializeRuins2dData>? _itemhook;

    private Config.SkipAnimationSetting _skipDrillAnimation;
    private bool _skipItemFindAnimation;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;
        _skipDrillAnimation = context.Configuration.SkipDrill;
        _skipItemFindAnimation = context.Configuration.SkipItemFind;

        _modLoader.GetController<IHooks>().TryGetTarget(out var hooks);
        if (hooks == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get hook controller.", Color.Red);
            return;
        }

        InitializeDrillSkip();

        hooks.AddHook<InitializeRuins2dData>(ShouldSkipItemFind)
            .ContinueWith(result => { _itemhook = result.Result; });

        hooks.CreateWrapper<CheckIfMonsterGrabbedAnItem>().ContinueWith(result => _itemfunc = result.Result);
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
        _skipDrillAnimation = configuration.SkipDrill;
        _skipItemFindAnimation = configuration.SkipItemFind;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    private void ShouldSkipItemFind(nint self)
    {
        // Call the original init function first
        _itemhook!.OriginalFunction.Invoke(self);

        // If we aren't trying to skip the animation, then early return
        if (!_skipItemFindAnimation) return;

        // Now call the function that actually "finds" the item or not based on the INT stat
        _itemfunc!.Invoke(self);

        // The number at offset 4 seems to relate to the monster's current animation state.
        // If we bump this to 0x0b, then the function will increment it to 0x0c and then it seems to complete early
        Memory.Instance.Write(nuint.Add((nuint)self, 4), 0x0b);
    }

    private void InitializeDrillSkip()
    {
        if (_hooks == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not initialize drill skip: hooks are unavailable.", Color.Red);
            return;
        }

        // Get the base address and size of the main module for later memory scanning
        using var process = Process.GetCurrentProcess();
        var module = process.MainModule;
        if (IntPtr.Size != 4 || module == null)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not initialize drill skip: unsupported game process.", Color.Red);
            return;
        }

        // Calculate the base address and size of the main module
        var exeBase = (nuint)module.BaseAddress;
        var moduleSize = module.ModuleMemorySize;

        // Scanner for locating function signatures in the main module
        var startupScanner = _modLoader.GetController<IStartupScanner>();
        if (startupScanner == null || !startupScanner.TryGetTarget(out var scanner))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not initialize drill skip: startup scanner is unavailable.", Color.Red);
            return;
        }

        // Initialize variables for scanning and storing addresses
        var scanLock = new object();
        var addresses = new nuint[4];
        var scansCompleted = 0;
        var scanFailed = false;

        // Helper function to check if an address is within the main module
        bool IsInModule(nuint address, int size)
        {
            var start = (ulong)exeBase;
            var value = (ulong)address;
            return value >= start && value + (ulong)size <= start + (ulong)moduleSize;
        }

        // Callback function to handle signature scan
        void CompleteScan(bool found, int offset, int index, string name)
        {
            // Do only 1 scan at a time
            lock (scanLock)
            {
                if (scanFailed) return;
                if (!found || offset < 0 || offset >= moduleSize)
                {
                    scanFailed = true;
                    _logger.WriteLine($"[{_modConfig.ModId}] Could not initialize drill skip: {name} signature not found.", Color.Red);
                    return;
                }

                // Store the found address and check if all scans are completed
                addresses[index] = exeBase + (nuint)offset;
                scansCompleted++;
                if (scansCompleted != addresses.Length) return;

                // These instructions contain relocated absolute pointers, not the pointed-to data
                if (!IsInModule(addresses[2] + pointerOffset, sizeof(uint)) ||
                    !IsInModule(addresses[3] + pointerOffset, sizeof(uint)))
                {
                    _logger.WriteLine($"[{_modConfig.ModId}] Could not initialize drill skip: invalid pointer operands.", Color.Red);
                    return;
                }

                // Read the actual addresses from the pointer operands
                Memory.Instance.Read(addresses[2] + pointerOffset, out uint drillVtable);
                Memory.Instance.Read(addresses[3] + pointerOffset, out uint pressedButtons);
                if (!IsInModule((nuint)drillVtable, sizeof(uint)) || !IsInModule((nuint)pressedButtons, sizeof(byte)))
                {
                    _logger.WriteLine($"[{_modConfig.ModId}] Could not initialize drill skip: invalid game data addresses.", Color.Red);
                    return;
                }

                _drillMode = (nuint)drillVtable;
                _pressedButtons = (nuint)pressedButtons;

                _AnimationFinish = _hooks.CreateWrapper<AnimationFinish>((long)addresses[1], out _);
                _drillAnimationHook = _hooks.CreateHook<AnimationStart>(HandleDrillAnimation,
                    (long)addresses[0]);
                _drillAnimationHook.Activate();
            }
        }

        scanner.AddMainModuleScan(Utils.GetSignature<AnimationStart>(BaseGame.Mr2, Region.Us),
            result => CompleteScan(result.Found, result.Offset, 0, "drill start"));
        scanner.AddMainModuleScan(Utils.GetSignature<AnimationFinish>(BaseGame.Mr2, Region.Us),
            result => CompleteScan(result.Found, result.Offset, 1, "animation finish"));
        scanner.AddMainModuleScan(drillSignature,
            result => CompleteScan(result.Found, result.Offset, 2, "drill vtable"));
        scanner.AddMainModuleScan(buttonPress,
            result => CompleteScan(result.Found, result.Offset, 3, "pressed buttons"));
    }

    private int HandleDrillAnimation(nint mode, nint child, nint context)
    {
        // Check if drill skip is enabled
        var setting = _skipDrillAnimation;
        if (setting == Config.SkipAnimationSetting.Auto || setting == Config.SkipAnimationSetting.Manual)
        {
            // Check if drill is currently ongoing
            Memory.Instance.Read((nuint)mode, out nuint vtable);
            if (vtable == _drillMode)
            {
                // Drill skip toggled to auto
                var shouldSkip = setting == Config.SkipAnimationSetting.Auto;

                // If drill skip is toggled to manual, check the back button state
                if (setting == Config.SkipAnimationSetting.Manual)
                {
                    Memory.Instance.Read(_pressedButtons, out byte buttons);
                    shouldSkip = (buttons & backButton) != 0;
                }

                // Jump to animation finish if should skip
                if (shouldSkip)
                {
                    Memory.Instance.Read((nuint)context + 0x2C, out nint animation);
                    Memory.Instance.Read((nuint)animation + 0x7C, out int finished);
                    if (finished == 0)
                        _AnimationFinish!(animation);
                }
            }
        }

        return _drillAnimationHook!.OriginalFunction(mode, child, context);
    }
}
