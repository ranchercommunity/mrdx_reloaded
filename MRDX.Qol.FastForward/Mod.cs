using MRDX.Qol.FastForward.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory.Sources;

namespace MRDX.Qol.FastForward
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

        private const nint UserInputOffset = 0x3723B0;
        private const nint FastForwardOffset = 0x1D01EA1;
        private const int LeftTrigger = 0x1;
        private static nuint _inputPtr;
        private static nuint _ffPtr;
        private bool wasPressed = false;

        private const string RegisterUserInputSignature = "55 8B EC 83 EC 10 53 56 57 8D 4D F0";

        private IHook<RegisterUserInput>? _hook;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _modConfig = context.ModConfig;


            // For more information about this template, please see
            // https://reloaded-project.github.io/Reloaded-II/ModTemplate/

            // If you want to implement e.g. unload support in your mod,
            // and some other neat features, override the methods in ModBase.

            var thisProcess = Process.GetCurrentProcess();
            var baseAddr = thisProcess.MainModule!.BaseAddress;
            var baseAddress = new UIntPtr(BitConverter.ToUInt64(BitConverter.GetBytes(baseAddr.ToInt64()), 0));
            _inputPtr = UIntPtr.Add(baseAddress, UserInputOffset.ToInt32());
            _ffPtr = UIntPtr.Add(baseAddress, FastForwardOffset.ToInt32());

            _modLoader.GetController<IStartupScanner>().TryGetTarget(out var startupScanner);
            startupScanner?.AddMainModuleScan(RegisterUserInputSignature,
                result => _hook = _hooks!.CreateHook<RegisterUserInput>(ToggleFastForward, (long)baseAddress + result.Offset).Activate());
        }

        private char ToggleFastForward()
        {
            char result = _hook.OriginalFunction();
            
            Memory.Instance.Read<int>(_inputPtr, out var input);
            Memory.Instance.Read<byte>(_ffPtr, out var ffFlag);

            bool isPressed = (input & LeftTrigger) != 0;

            if (wasPressed == true && ! isPressed)
            {
                ffFlag = (byte) (ffFlag == 1 ? 0 : 1);
            }

            wasPressed = isPressed;

            Memory.Instance.Write<byte>(_ffPtr, ref ffFlag);

            return result;
        }

        [Function(CallingConventions.Stdcall)]
        private delegate char RegisterUserInput();
        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}