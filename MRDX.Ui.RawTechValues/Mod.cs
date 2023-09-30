using MRDX.Base.Mod.Interfaces;
using MRDX.Ui.RawTechValues.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory;
using Reloaded.Mod.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace MRDX.Ui.RawTechValues;

/// <summary>
///     Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    private const short ForceYCoord = -18;
    private const int HitChanceOffset = 0x2A;
    private const short HitChanceYCoord = 6;
    private const int SharpnessOffset = 0x34;
    private const short SharpnessYCoord = 54;
    private const int WitheringOffset = 0x2B;
    private const short WitheringYCoord = 30;

    private const short TechValueXCoord = 105;

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

    private DrawIntWithHorizontalSpacing? _drawInt;

    private IHook<DrawMonsterCardForceValue>? _forceHook;
    private IHook<DrawMonsterCardHitChanceValue>? _hitChanceHook;
    private IHook<DrawMonsterCardSharpnessValue>? _sharpnessHook;
    private IHook<DrawMonsterCardWitheringValue>? _witheringHook;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;

        _modLoader.GetController<IHooks>().TryGetTarget(out var hooks);
        hooks!.AddHook<DrawMonsterCardHitChanceValue>(MonsterHitChanceRawNum)
            .ContinueWith(result => _hitChanceHook = result.Result.Activate());
        hooks!.AddHook<DrawMonsterCardForceValue>(MonsterForceRawNum)
            .ContinueWith(result => _forceHook = result.Result.Activate());
        hooks!.AddHook<DrawMonsterCardSharpnessValue>(MonsterSharpnessRawNum)
            .ContinueWith(result => _sharpnessHook = result.Result.Activate());
        hooks!.AddHook<DrawMonsterCardWitheringValue>(MonsterWitheringRawNum)
            .ContinueWith(result => _witheringHook = result.Result.Activate());
        hooks.CreateWrapper<DrawIntWithHorizontalSpacing>().ContinueWith(result => _drawInt = result.Result);
    }

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion

    private int MonsterHitChanceRawNum(nint self)
    {
        var techInfoPointer = GetTechInfoPointer(self);
        Memory.Instance.Read<sbyte>(nuint.Add(techInfoPointer, HitChanceOffset), out var hitChance);
        return _drawInt!(GetXCoord(hitChance), HitChanceYCoord, hitChance);
    }

    private int MonsterForceRawNum(nint self, sbyte force, int unk)
    {
        return _drawInt!(GetXCoord(force), ForceYCoord, force);
    }

    private int MonsterSharpnessRawNum(nint self)
    {
        var techInfoPointer = GetTechInfoPointer(self);
        Memory.Instance.Read<sbyte>(nuint.Add(techInfoPointer, SharpnessOffset), out var sharpness);
        return _drawInt!(GetXCoord(sharpness), SharpnessYCoord, sharpness);
    }

    private int MonsterWitheringRawNum(nint self)
    {
        var techInfoPointer = GetTechInfoPointer(self);
        Memory.Instance.Read<sbyte>(nuint.Add(techInfoPointer, WitheringOffset), out var withering);
        return _drawInt!(GetXCoord(withering), WitheringYCoord, withering);
    }

    private nuint GetTechInfoPointer(nint self)
    {
        Memory.Instance.Read<nuint>(nuint.Add((nuint)self, 0x10C), out var techInfoPointer);
        return techInfoPointer;
    }

    private short GetXCoord(sbyte techVal)
    {
        var offset = techVal.ToString().Length - 1;
        return (short)(TechValueXCoord - 3 * offset);
    }
}