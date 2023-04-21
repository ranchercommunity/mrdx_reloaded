using MRDX.Base.Mod.Interfaces;
using MRDX.Ui.RawTechValues.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace MRDX.Ui.RawTechValues
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
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        private readonly Int16 _techValueXCoord = 101;

        private IHook<DrawMonsterCardHitChanceValue> _hitChanceHook;
        private readonly Int16 _hitChanceYCoord = 6;
        private readonly int _hitChanceOffset = 0x2A;

        private IHook<DrawMonsterCardForceValue> _forceHook;
        private readonly Int16 _forceYCoord = -18;

        private IHook<DrawMonsterCardSharpnessValue> _sharpnessHook;
        private readonly Int16 _sharpnessYCoord = 54;
        private readonly int _sharpnessOffset = 0x34;

        private IHook<DrawMonsterCardWitheringValue> _witheringHook;
        private readonly Int16 _witheringYCoord = 30;
        private readonly int _witheringOffset = 0x2B;

        private IHook<DrawIntWithHorizontalSpacing> _drawInt;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _modConfig = context.ModConfig;


            _modLoader.GetController<IHooks>().TryGetTarget(out var hooks);
            hooks!.AddHook<DrawMonsterCardHitChanceValue>(MonsterHitChanceRawNum).ContinueWith(result => _hitChanceHook = result.Result?.Activate());
            hooks!.AddHook<DrawMonsterCardForceValue>(MonsterForceRawNum).ContinueWith(result => _forceHook = result.Result?.Activate());
            hooks!.AddHook<DrawMonsterCardSharpnessValue>(MonsterSharpnessRawNum).ContinueWith(result => _sharpnessHook = result.Result?.Activate());
            hooks!.AddHook<DrawMonsterCardWitheringValue>(MonsterWitheringRawNum).ContinueWith(result => _witheringHook = result.Result?.Activate());
            // No idea how to use CreateWrapper to grab this function (I ran into a StackOverflow on runtime),
            // so I'm hooking a dummy function as a way to get a reference to the actual function
            hooks!.AddHook<DrawIntWithHorizontalSpacing>(Draw).ContinueWith(result => _drawInt = result.Result?.Activate());
        }

        private int Draw(Int16 x, Int16 y, int number, Int16 horizontalSpacing)
        {
            return _drawInt.OriginalFunction(x, y, number, horizontalSpacing);
        }

        private int MonsterHitChanceRawNum(nint self)
        {
            var techInfoPointer = GetTechInfoPointer(self);
            Memory.Instance.Read<sbyte>(nuint.Add(techInfoPointer, _hitChanceOffset), out var hitChance);
            return _drawInt.OriginalFunction(GetXCoord(hitChance), _hitChanceYCoord, hitChance, 0);
        }

        private int MonsterForceRawNum(nint self, sbyte force, int unk)
        {
            return _drawInt.OriginalFunction(GetXCoord(force), _forceYCoord, force, 0);
        }

        private int MonsterSharpnessRawNum(nint self)
        {
            var techInfoPointer = GetTechInfoPointer(self);
            Memory.Instance.Read<sbyte>(nuint.Add(techInfoPointer, _sharpnessOffset), out var sharpness);
            return _drawInt.OriginalFunction(GetXCoord(sharpness), _sharpnessYCoord, sharpness, 0);
        }

        private int MonsterWitheringRawNum(nint self)
        {
            var techInfoPointer = GetTechInfoPointer(self);
            Memory.Instance.Read<sbyte>(nuint.Add(techInfoPointer, _witheringOffset), out var withering);
            return _drawInt.OriginalFunction(GetXCoord(withering), _witheringYCoord, withering, 0);
        }

        private nuint GetTechInfoPointer(nint self)
        {
            Memory.Instance.Read<nuint>(nuint.Add((nuint)self, 0x10C), out var techInfoPointer);
            return techInfoPointer;
        }

        private Int16 GetXCoord(sbyte techVal)
        {
            if (techVal < 0 || techVal > 9)
                return (Int16) (_techValueXCoord - 3);
            return _techValueXCoord;
        }
        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}